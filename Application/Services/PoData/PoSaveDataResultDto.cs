using System;
using System.Collections.Generic;

namespace Application.Services.PoData
{
    public class PoSaveDataResultDto
    {
        public List<PoSaveDataOutput> Results { get; set; }
    }
    public class PoSaveDataOutput
    {
        public PoSaveDataOutput()
        {

        }

        public PoSaveDataOutput(string poNumber, DateTime? confirmDate, DateTime? statusDate, DateTime? bookingDate, string message, bool factoryStatusNeedsToHaveReadyToGO, double? rate)
        {
            PoNumber = poNumber;
            ConfirmDate = confirmDate;
            StatusDate = statusDate;
            BookingDate = bookingDate;
            Message = message;
            FactoryStatusNeedsToHaveReadyToGO = factoryStatusNeedsToHaveReadyToGO;
            Rate = rate;
        }

        public string PoNumber { get; set; }
        public DateTime? ConfirmDate { get; set; }
        public DateTime? StatusDate { get; set; }
        public DateTime? BookingDate { get; set; }
        public bool FactoryStatusNeedsToHaveReadyToGO { get; set; }
        public double? Rate { get; set; }

        public string Message { get; set; }

    }

}
