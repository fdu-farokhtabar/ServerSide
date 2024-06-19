using Application.Services.Account;

namespace Application.Services.PoData
{
    public class PoSecurityData
    {
        public ColPermission User { get; set; }
        public ColPermission Date { get; set; }
        public ColPermission CustomerPO { get; set; }
        public ColPermission EstimateNumber { get; set; }
        public ColPermission Name { get; set; }
        public ColPermission PONumber { get; set; }
        public ColPermission DueDate { get; set; }
        public ColPermission ItemGroup { get; set; }
        public ColPermission Forwarder { get; set; }
        public ColPermission IOR { get; set; }
        public ColPermission ShipTo { get; set; }
        public ColPermission ShippingCarrier { get; set; }
        public ColPermission ContainerNumber { get; set; }
        public ColPermission ETAAtPort { get; set; }


        public ColPermission FactoryStatus { get; set; }
        public ColPermission StatusDate { get; set; }
        public ColPermission FactoryContainerNumber { get; set; }
        public ColPermission FactoryBookingDate { get; set; }
        public ColPermission DocumentsSendOutDate { get; set; }
        //Forwarder
        public ColPermission ForwarderName { get; set; }
        public ColPermission BookingDate { get; set; }
        public ColPermission Rate { get; set; }
        public ColPermission ETD { get; set; }
        public ColPermission ETA { get; set; }
        public ColPermission PortOfDischarge { get; set; }
        public ColPermission DischargeStatus { get; set; }
        //Shawn
        public ColPermission ShippmentStatus { get; set; }
        public ColPermission ConfirmDate { get; set; }
        //Warehouse Record
        public ColPermission GateIn { get; set; }
        public ColPermission EmptyDate { get; set; }
        public ColPermission GateOut { get; set; }
        //Ap
        public ColPermission BillDate { get; set; }

        public ColPermission Note { get; set; }
    }
    public class ColPermission
    {
        public ColPermission(string colRoles, string writable, AuthorizationService authorizationService)
        {
            HasAccess = authorizationService.HasUserPermissionToUseData(colRoles);
            Writable = authorizationService.IsWritableColumn(writable, colRoles);
        }
        public bool HasAccess { get; set; }
        public bool Writable { get; set; }
    }

}
