using Domain.Entity;
using System;

namespace Application.Services.PoData
{
    public class PoDataDto
    {
        //Excel
        public string User { get; set; }
        public string Date { get; set; }
        public string CustomerPO { get; set; }
        public string EstimateNumber { get; set; }
        public string Name { get; set; }
        public string PONumber { get; set; }
        public string DueDate { get; set; }
        public string ItemGroup { get; set; }
        public string Forwarder { get; set; }
        public string IOR { get; set; }
        public string ShipTo { get; set; }
        public string ShippingCarrier { get; set; }
        public string ContainerNumber { get; set; }
        public string ETAAtPort { get; set; }
        //Db

        //Factory
        public FactoryStatus? FactoryStatus { get; set; }
        /// <summary>
        /// در زمانی که تاریخ 
        /// ETD
        /// کمتر از 14 روز باشد 
        /// بایستی این گزنیه فعال شود
        /// </summary>
        public bool FactoryStatusNeedsToHaveReadyToGO { get; set; }
        public DateTime? StatusDate { get; set; }
        public string FactoryContainerNumber { get; set; }
        public DateTime? FactoryBookingDate { get; set; }
        public DateTime? DocumentsSendOutDate { get; set; }
        //Forwarder
        public ForwarderName? ForwarderName { get; set; }
        public DateTime? BookingDate { get; set; }
        public double? Rate { get; set; }
        public DateTime? ETD { get; set; }
        public DateTime? ETA { get; set; }
        public string PortOfDischarge { get; set; }
        public DischargeStatus? DischargeStatus { get; set; }
        //Shawn
        public ShippmentStatus? ShippmentStatus { get; set; }
        public DateTime? ConfirmDate { get; set; }
        //Warehouse Record
        public DateTime? GateIn { get; set; }
        public DateTime? EmptyDate { get; set; }
        public DateTime? GateOut { get; set; }
        //Ap
        /// <summary>
        /// در صورتی که این فیلد مقدار داشته باشد یعنی بایگانی
        /// </summary>
        public DateTime? BillDate { get; set; }

        /// <summary>
        /// سطح دسترسی مربوط به نمایش این سطر
        /// لیست نقش های مجاز
        /// </summary>
        public string Rep { get; set; }
        public string Note { get; set; }
    }
}
