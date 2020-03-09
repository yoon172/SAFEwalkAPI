﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SafewalkApplication.Contracts;
using SafewalkApplication.Models;
using SafewalkApplication.Helpers;

#nullable enable
namespace SafewalkApplication.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WalksController : ControllerBase
    {
        private readonly IWalkRepository _walkRepository;
        private readonly IUserRepository _userRepository;
        private readonly ISafewalkerRepository _safewalkerRepository;

        public WalksController(IWalkRepository walkRepository, IUserRepository userRepository, ISafewalkerRepository safewalkerRepository)
        {
            _walkRepository = walkRepository;
            _safewalkerRepository = safewalkerRepository;
            _userRepository = userRepository;
        }

        // GET: api/Walks
        // Authorization: Safewalker
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Walk>>> GetWalks([FromHeader] string? token, [FromHeader] string? email)
        {
            if (token == null) // if no token
            {
                return BadRequest();
            }

            // if not signed in and authenticated
            if (!(await _safewalkerRepository.Authenticated(token, email)))
            {
                return Unauthorized();
            }

            IEnumerable<Walk> walkList = _walkRepository.GetAll();

            if (!walkList.Any<Walk>()) // if there are no walks 
            {
                return NotFound();
            }
            
            return Ok(new ObjectResult(walkList));
        }

        // GET: api/Walks/{id}
        // Authorization: User, Safewalker
        [HttpGet("{id}")]
        public async Task<ActionResult<Walk>> GetWalk([FromHeader] string? token, [FromHeader] string? email, 
            [FromRoute] string? id, [FromHeader] bool? isUser)
        {
            if (token == null || id == null || isUser == null) // if empty field
            {
                return BadRequest();
            }

            // if user and not authenticated
            if ((bool)isUser && !await _userRepository.Authenticated(token, email)) 
            {
                return Unauthorized();
            }
            // is safewalker and not authenticated
            else if ((bool)!isUser && !await _safewalkerRepository.Authenticated(token, email))
            {
                return Unauthorized();
            }
            
            var walk = await _walkRepository.Get(id);
            if (walk == null)
            {
                return NotFound();
            }

            // Check if person is part of the walk
            if ((bool)isUser && walk.UserEmail != email)
            {
                return Unauthorized();
            }
            else if ((bool)!isUser && walk.WalkerEmail != email)
            {
                return Unauthorized();
            }

            return Ok(walk);
        }

        // GET: api/Walks/{id}/status
        // Authorization: User, Safewalker
        [HttpGet("{id}/status")]
        public async Task<ActionResult<int>> GetWalkStatus([FromHeader] string? token, [FromHeader] string? email,
            [FromRoute] string? id, [FromHeader] bool? isUser)
        {
            if (token == null || id == null || isUser == null) // if empty field
            {
                return BadRequest();
            }

            // if user and not authenticated
            if ((bool)isUser && !await _userRepository.Authenticated(token, email))
            {
                return Unauthorized();
            }
            // is safewalker and not authenticated
            else if ((bool)!isUser && !await _safewalkerRepository.Authenticated(token, email))
            {
                return Unauthorized();
            }

            var walk = await _walkRepository.Get(id);
            var status = walk.Status;
            if (walk == null || status == null)
            {
                return NotFound();
            }

            // Check if person is part of the walk
            if ((bool)isUser && walk.UserEmail != email)
            {
                return Unauthorized();
            }
            else if ((bool)!isUser && walk.WalkerEmail != email)
            {
                return Unauthorized();
            }

            return Ok(status);
        }

        // GET: api/Walks/{id}/location
        // Authorization: User, Safewalker
        [HttpGet("{id}/location")]
        public async Task<ActionResult<Walk>> GetWalkerLocation([FromHeader] string? token, [FromHeader] string? email,
            [FromRoute] string? id, [FromHeader] bool? isUser)
        {
            if (token == null || id == null || isUser == null) // if empty field
            {
                return BadRequest();
            }

            // if user and not authenticated
            if ((bool)isUser && !await _userRepository.Authenticated(token, email))
            {
                return Unauthorized();
            }
            // is safewalker and not authenticated
            else if ((bool)!isUser && !await _safewalkerRepository.Authenticated(token, email))
            {
                return Unauthorized();
            }

            var walk = await _walkRepository.Get(id);
            var currLat = walk.WalkerCurrLat;
            var currLng = walk.WalkerCurrLng;
            if (walk == null || currLat == null || currLng == null)
            {
                return NotFound();
            }

            // Check if person is part of the walk
            if ((bool)isUser && walk.UserEmail != email)
            {
                return Unauthorized();
            }
            else if ((bool)!isUser && walk.WalkerEmail != email)
            {
                return Unauthorized();
            }

            var walkerLocation = new { lat = currLat, lng = currLng };
            return Ok(walkerLocation);
        }

        // PUT: api/Walks/{id}
        // Authorization: User, Safewalker
        // User Field Access: Status
        // Safewalker Field Access: Status, WalkerEmail
        [HttpPut("{id}")]
        public async Task<IActionResult> PutWalk([FromHeader] string? token, [FromHeader] string? email,
            [FromRoute] string? id, [FromHeader] bool? isUser, [FromBody] Walk walk)
        {
            if (token == null || id == null || isUser == null) // if empty field
            {
                return BadRequest();
            }

            // if user and not authenticated
            if ((bool)isUser && !await _userRepository.Authenticated(token, email))
            {
                return Unauthorized();
            }
            // is safewalker and not authenticated
            else if ((bool)!isUser && !await _safewalkerRepository.Authenticated(token, email))
            {
                return Unauthorized();
            }

            var oldWalk = await _walkRepository.Get(id);
            if (oldWalk == null)
            {
                return NotFound();
            }

            // Check if person is part of the walk
            if ((bool)isUser && oldWalk.UserEmail != email)
            {
                return Unauthorized();
            }
            else if ((bool)!isUser && oldWalk.WalkerEmail != email)
            {
                return Unauthorized();
            }

            // map fields from input walk to existing walk
            if ((bool)isUser)
            {
                oldWalk.MapFieldsUser(walk);
            } 
            else
            {
                oldWalk.MapFieldsWalker(walk);
            }

            await _walkRepository.Update(id, oldWalk);
    
            return Ok();
        }

        // POST: api/Walk
        // Authorization: User
        // Field Access: 
        [HttpPost]
        public async Task<ActionResult<Walk>> PostWalk([FromHeader] string? token, [FromBody] Walk? walk)
        {
            if (token == null || walk == null ) // if empty field
            {
                return BadRequest();
            }

            Guid guid = Guid.NewGuid();
            walk.Id = guid.ToString();
            walk.Status = 0;
            await _walkRepository.Add(walk);
            return Ok(walk);
        }

    }
}
