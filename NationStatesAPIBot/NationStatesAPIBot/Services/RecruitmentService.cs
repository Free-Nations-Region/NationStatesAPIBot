using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NationStatesAPIBot.Commands.Management;
using NationStatesAPIBot.Entities;

namespace NationStatesAPIBot.Services
{
    public class RecruitmentService
    {
        public bool IsReceivingRecruitableNation { get; internal set; }

        public void StartRecruitment()
        {
            throw new NotImplementedException();
        }

        public void StopRecruitment()
        {
            throw new NotImplementedException();
        }

        public void StartReceiveRecruitableNations(RNStatus currentRN)
        {
            throw new NotImplementedException();
        }
        public void StopReceiveRecruitableNations()
        {
            throw new NotImplementedException();
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
    }
}
