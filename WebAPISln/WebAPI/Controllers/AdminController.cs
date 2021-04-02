﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using WebAPI.DataAccess;
using WebAPI.Helpers;
using WebAPI.Models;

namespace WebAPI.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize(AuthenticationSchemes ="Admin")]
    public class AdminController : ControllerBase
    {
        private SqlServerDataAccess _dataAccess;
        private TokenHelper _token;

        public AdminController(IConfiguration config)
        {
            _dataAccess = new SqlServerDataAccess(config);
            _token = new TokenHelper(config);
        }

        private int GetIdFromToken()
        {
            var identity = HttpContext.User.Identity as ClaimsIdentity;
            IList<Claim> claim = identity.Claims.ToList();
            return Convert.ToInt32(claim[0].Value);
        }

        [AllowAnonymous]
        [HttpPost]
        public IActionResult Login([FromBody] AdminModel model)
        {
            int adminId = _dataAccess.IsAdminLoginValid(model.Name, model.HashedPw);
            if (adminId == 0)
            {
                return Unauthorized();
            }
            else
            {
                return Ok(new { token = _token.GenerateJSONWebToken(adminId,true)});
            }
        }

        /// <summary>
        /// create new election with information of election description and candidates information 
        /// (cannot be updated after creation)
        /// (you can add voters after creation)
        /// </summary>
        [HttpPut]
        public IActionResult CreateNewElection([FromBody] CreateElectionModel election)
        {
            int adminId = GetIdFromToken();
            election.electionDetails.AdminId = adminId;
            if (election.electionDetails.Description != null && election.electionDetails.Description.Length > 3 && election.electionDetails.Header != null && election.electionDetails.Header.Length > 3)
            {
                int insertedElectionId = _dataAccess.InsertElection(election.electionDetails);
                foreach (var candidate in election.candidates)
                {
                    candidate.ElectionId = insertedElectionId;
                    _dataAccess.InsertCandidateToElection(candidate);
                }
                return Ok();
            }
            return BadRequest("description is too short or null");
        }

        [HttpPost]
        public IActionResult CompleteTheElection([FromQuery] int electionId)
        {
            int adminId = GetIdFromToken();
            if (_dataAccess.IsAdminCreatorOfSpecifiedElection(adminId,electionId))
            {
                _dataAccess.UpdateElectionStatus(true,electionId);
                return Ok();
            }
            return BadRequest("you are not the creator of the specified election, dont trick us !");
        }

        /// <summary>
        /// send voter email for the specified election
        /// email and hashed password will automatically send to added voter's mail adress manually
        /// </summary>
        [HttpPut]
        public IActionResult AddVoterToElection([FromBody] AddVoterToElectionModelDTO addModel)
        {
            int adminId = GetIdFromToken();
            
            if (_dataAccess.IsAdminCreatorOfSpecifiedElection(adminId, addModel.ElectionId))
            {
                if (_dataAccess.GetElectionDetailsFromId(addModel.ElectionId).IsCompleted == true)
                {
                    return BadRequest("Election completed/finished, cannot add new voter !");
                }
                string plainPw = RandomPassword.GenerateRandomPassword();
                string hashedPw = HashingHelper.EncryptSHA256(plainPw);
                try
                {
                    _dataAccess.InsertVoterToSpecifiedElection(new AddVoterToElectionModel() { dtoModel = addModel, HashedPw = hashedPw });
                }
                catch (Exception ex)
                {
                    var exception = ex;
                    return BadRequest("cannot add voter to election, voter might already added to the election");
                }
                SendEmailModel model = new SendEmailModel { Subject = $"Your Password For Election {_dataAccess.GetElectionNameFromId(addModel.ElectionId)}", Body = plainPw, To = addModel.Email };
                bool isSendSuccess = EmailHelper.Send(model);
                if (!isSendSuccess)
                {
                    _dataAccess.DeleteVoterFromElection(addModel.ElectionId,addModel.Email);
                    return BadRequest("email cannot send, failed adding voter to election");
                }
                return Ok("voter added and password sended to specified email adress");
            }
            return BadRequest("you are not admin of the specified election");
        }

        /// <summary>
        /// delete specified voter from the election if voter didnt vote yet
        /// </summary>
        [HttpDelete]
        public IActionResult DeleteVoterFromElection([FromBody] DeleteVoterFromElectionModel model)
        {
            if (_dataAccess.IsAdminCreatorOfSpecifiedElection(GetIdFromToken(), model.ElectionId) && !_dataAccess.IsUserVoted(model.ElectionId,model.Email))
            {
                _dataAccess.DeleteVoterFromElection(model.ElectionId,model.Email);
                return Ok();
            }
            return BadRequest("you are not admin of the specified election");
        }

        /// <summary>
        /// returns list of election which are created by the admin
        /// </summary>
        [HttpGet]
        public IActionResult GetCreatedElections()
        {
            var electionsCreatedByAdmin = _dataAccess.GetAllElections().Where(x => x.AdminId == GetIdFromToken());
            return Ok(electionsCreatedByAdmin);
        }

        /// <summary>
        /// returns list of voter for specified election
        /// </summary>
        [HttpGet]
        public IActionResult GetVotersOfElection([FromQuery] int electionId)
        {
            return Ok(_dataAccess.GetVotersOfElection(electionId));
        }
    }
}
