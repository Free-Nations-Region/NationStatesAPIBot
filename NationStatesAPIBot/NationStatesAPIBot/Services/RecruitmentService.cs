using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NationStatesAPIBot.Commands.Management;
using NationStatesAPIBot.Entities;

namespace NationStatesAPIBot.Services
{
    public class RecruitmentService
    {
        private readonly ILogger<RecruitmentService> _logger;


        private RNStatus currentRNStatus;

        public RecruitmentService(ILogger<RecruitmentService> logger)
        {
            _logger = logger;
        }

        public bool IsReceivingRecruitableNation { get; internal set; }
        public bool IsRecruiting { get; private set; }

        public void StartRecruitment()
        {
            
            IsRecruiting = true;
            Task.Run(async () => await RecruitAsync());
        }

        public void StopRecruitment()
        {
            throw new NotImplementedException();
        }

        public void StartReceiveRecruitableNations(RNStatus currentRN)
        {
            currentRNStatus = currentRN;
            IsReceivingRecruitableNation = true;
        }
        public void StopReceiveRecruitableNations()
        {
            currentRNStatus = null;
            IsReceivingRecruitableNation = false;
        }

        public async Task<List<Nation>> GetRecruitableNations(int number)
        {
            throw new NotImplementedException();
        }

        public async Task SetNationStatusToAsync(Nation nation, string statusName)
        {
            throw new NotImplementedException();
        }


        private async Task<bool> WouldReceiveTelegram()
        {
            throw new NotImplementedException();
        }

        private async void GetNewNationsAsync()
        {
            throw new NotImplementedException();
        }

        private async Task RecruitAsync()
        {
            throw new NotImplementedException();
        }

        private async Task SendTelegramAsync()
        {
            throw new NotImplementedException();
        }

        internal string GetRNStatus()
        {
            if (IsReceivingRecruitableNation)
            {
                return currentRNStatus.ToString();
            }
            else
            {
                return "No /rn command currently running.";
            }
        }
    }
}
