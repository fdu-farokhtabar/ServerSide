using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Application.Services.PoData
{
    public class PoDataForExcelExportDtoMap : ClassMap<PoDataDto>
    {
        public static List<ColAccess> ColumnsHavePermission { get; set; }
        public PoDataForExcelExportDtoMap()
        {
            int index = 0;

            MapColumn(m => m.User, "User", "User", index++);
            MapColumn(m => m.Date, "Date", "Order Date", index++);
            MapColumn(m => m.CustomerPO, "Customer PO", "Customer PO", index++);
            MapColumn(m => m.EstimateNumber, "Estimate Number", "Estimate #", index++);
            MapColumn(m => m.Name, "Name", "Name", index++);
            MapColumn(m => m.PONumber, "PO Number", "KIAN PO #", index++);
            MapColumn(m => m.DueDate, "Due Date", "ETD Request", index++);
            MapColumn(m => m.ItemGroup, "Item Group", "Item Group", index++);
            MapColumn(m => m.Forwarder, "Forwarder", "Forwarder", index++);
            MapColumn(m => m.IOR, "IOR", "IOR", index++);
            MapColumn(m => m.ShipTo, "Ship To", "Ship To", index++);
            MapColumn(m => m.ShippingCarrier, "Shipping Carrier", "Shipping Carrier", index++);
            MapColumn(m => m.ContainerNumber, "Container Number", "Container Number", index++);
            MapColumn(m => m.ETAAtPort, "ETA at Port", "ETA at Port", index++);
            MapColumn(m => m.FactoryStatus, "Factory Status", "Status", index++);
            MapColumn(m => m.StatusDate, "Status Date", "Status Date", index++);
            MapColumn(m => m.FactoryContainerNumber, "Factory Container Number", "Container #", index++);
            MapColumn(m => m.FactoryBookingDate, "Factory Booking Date", "Factory Booking Date", index++);
            MapColumn(m => m.DocumentsSendOutDate, "Doc Send Out Date", "Doc Send Out Date", index++);
            MapColumn(m => m.ForwarderName, "Forwarder Name", "Forwarder Name", index++);
            MapColumn(m => m.BookingDate, "Booking Date", "Booking Date", index++);
            MapColumn(m => m.Rate, "Rate", "Rate", index++);
            MapColumn(m => m.ETD, "ETD", "ETD", index++);
            MapColumn(m => m.ETA, "ETA", "ETA", index++);
            MapColumn(m => m.PortOfDischarge, "Port Of Discharge", "Port Of Discharge", index++);
            MapColumn(m => m.DischargeStatus, "Discharge Status", "Discharge Status", index++);
            MapColumn(m => m.ShippmentStatus, "Shipment Confirmation", "Shipment Confirmation", index++);
            MapColumn(m => m.ConfirmDate, "Confirm date", "Confirm date", index++);
            MapColumn(m => m.GateIn, "Gate In", "Gate In", index++);
            MapColumn(m => m.EmptyDate, "Empty Date", "Empty Date", index++);
            MapColumn(m => m.GateOut, "Gate out", "Gate out", index++);
            MapColumn(m => m.BillDate, "Bill Date", "Bill Date", index++);
            MapColumn(m => m.Note, "Note", "Note", index++);
        }

        public void MapColumn<T>(Expression<Func<PoDataDto, T>> property, string ColNameInPermission, string ColNameInExcel, int index)
        {

            //var name = ((MemberExpression)property.Body).Member.Name;
            if (ColumnsHavePermission.Any(x => string.Equals(ColNameInPermission, x.ColName)))
            {
                Map(property).Name(ColNameInExcel).Index(index);
            }
            else
            {
                Map(property).Ignore();
            }
        }
    }
}
