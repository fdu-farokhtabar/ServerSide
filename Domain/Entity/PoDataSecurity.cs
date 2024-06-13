namespace Domain.Entity
{
    public class PoDataSecurity
    {
        public int Id { get; set; }
        //Excel        
        public string PoNumber { get; set; }
        public string User { get; set; }
        public string Date { get; set; }
        public string CustomerPO { get; set; }
        public string EstimateNumber { get; set; }
        public string Name { get; set; }
        public string DueDate { get; set; }
        public string ItemGroup { get; set; }
        public string Forwarder { get; set; }
        public string IOR { get; set; }
        public string ShipTo { get; set; }
        public string ShippingCarrier { get; set; }
        public string ContainerNumber { get; set; }
        public string ETAAtPort { get; set; }


        //Factory
        public string FactoryStatus { get; set; }
        public string StatusDate { get; set; }
        public string FactoryContainerNumber { get; set; }
        public string BookingDate { get; set; }
        public string DocumentsSendOutDate { get; set; }
        //Forwarder
        public string ForwarderName { get; set; }
        public string FactoryBookingDate { get; set; }
        public string Rate { get; set; }
        public string ETD { get; set; }
        public string ETA { get; set; }
        public string PortOfDischarge { get; set; }
        public string DischargeStatus { get; set; }
        //Shawn
        public string ShippmentStatus { get; set; }
        public string ConfirmDate { get; set; }
        //Warehouse Record
        public string GateIn { get; set; }
        public string EmptyDate { get; set; }
        public string GateOut { get; set; }
        //Ap
        public string BillDate { get; set; }
        public string Note { get; set; }
    }
}
