﻿using EFDataAccessClassLib.Models;
using System.Collections.Generic;

namespace EFDataAccessClassLib.DataAccess
{
    public interface IEFDataAccess
    {
        void DeleteVoterFromElection(int electionId, string email);
        AdminModel GetAdminDetailsByAdminId(string id);
        List<ElectionModel> GetAllElections();
        List<CandidateModel> GetCandidatesOfElection(int electionId);
        ElectionModel GetElectionDetailsFromId(int electionId);
        string GetElectionNameFromId(int electionId);
        List<int> GetElectionsOfVoter(string email);
        List<string> GetVotersOfElection(int electionId);
        List<string> GetVotesOfElection(int electionId);
        void InsertCandidateToElection(CandidateModel candidate);
        int InsertElection(ElectionModel m);
        void InsertNewAdmin(string name, string hashedPw);
        void InsertVote(VoteModel model);
        void InsertVoterToSpecifiedElection(AddVoterToElectionModel model);
        bool IsAdminCreatorOfSpecifiedElection(int adminId, int electionId);
        int IsAdminLoginValid(string name, string hashedPw);
        bool IsUserVoted(int electionId, string email);
        int IsVoterLoginValid(string email, string hashedPw, int electionId);
        void UpdateElectionStatus(bool status, int electionId);
    }
}