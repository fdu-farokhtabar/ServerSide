using Application.Data;
using Application.Services.UpdateDataByExcel.Helper;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static Application.Services.UpdateDataByExcel.Helper.UpdateByExcelHelper;

namespace Application.Services.UpdateDataByExcel
{
    public class UpdatePoDataByExcelService
    {
        private readonly Stream stream;
        private readonly DataTableCollection tables;

        public UpdatePoDataByExcelService(Stream stream)
        {
            if (stream is null)
                throw new Exception("Please upload correct PoData excel file.");
            this.stream = stream;
            tables = UpdateByExcelHelper.ReadExcel(stream);
        }
        public async Task UpdateSecurity()
        {
            List<Domain.Entity.PoDataSecurity> newPoDataSecurity = new();
            try
            {
                if (tables?.Count > 0)
                {
                    if (tables[1].Rows?.Count > 0)
                    {
                        for (int i = 0; i < tables[1].Rows.Count; i++)
                        {
                            var Row = tables[1].Rows[i];
                            Domain.Entity.PoDataSecurity NewSecurity = new()
                            {
                                PoNumber = Row["PO Number"].ToString().Trim(),

                                User = Row["User"].ToString().Trim(),
                                Date = Row["Date"].ToString().Trim(),
                                CustomerPO = Row["Customer PO"].ToString().Trim(),
                                EstimateNumber = Row["Estimate Number"].ToString().Trim(),
                                Name = Row["Name"].ToString().Trim(),
                                DueDate = Row["Due Date"].ToString().Trim(),
                                ItemGroup = Row["Item Group"].ToString().Trim(),
                                Forwarder = Row["Forwarder"].ToString().Trim(),
                                IOR = Row["IOR"].ToString().Trim(),
                                ShipTo = Row["Ship To"].ToString().Trim(),
                                ShippingCarrier = Row["Shipping Carrier"].ToString().Trim(),
                                ContainerNumber = Row["Container Number"].ToString().Trim(),
                                ETAAtPort = Row["ETA at Port"].ToString().Trim(),


                                FactoryStatus = Row["Factory Status"].ToString().Trim(),
                                StatusDate = Row["Status Date"].ToString().Trim(),
                                FactoryContainerNumber = Row["Factory Container Number"].ToString().Trim(),
                                FactoryBookingDate = Row["Factory Booking Date"].ToString().Trim(),
                                DocumentsSendOutDate = Row["Doc Send Out Date"].ToString().Trim(),
                                ForwarderName = Row["Forwarder Name"].ToString().Trim(),
                                BookingDate = Row["Booking Date"].ToString().Trim(),
                                Rate = Row["Rate"].ToString().Trim(),
                                ETD = Row["ETD"].ToString().Trim(),
                                ETA = Row["ETA"].ToString().Trim(),
                                PortOfDischarge = Row["Port Of Discharge"].ToString().Trim(),
                                DischargeStatus = Row["Discharge Status"].ToString().Trim(),
                                ShippmentStatus = Row["Shippment Status"].ToString().Trim(),
                                ConfirmDate = Row["Confirm date"].ToString().Trim(),
                                GateIn = Row["Gate In"].ToString().Trim(),

                                EmptyDate = Row["Empty Date"].ToString().Trim(),
                                GateOut = Row["Gate out"].ToString().Trim(),
                                BillDate = Row["Bill Date"].ToString().Trim(),
                                Note = Row["Note"].ToString().Trim()
                            };
                            newPoDataSecurity.Add(NewSecurity);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("there are some errors during reading security data from PoData excel.[" + ex.Message + "]");
            }
            if (newPoDataSecurity?.Count > 0)
            {
                try
                {
                    using var db = new Context();
                    if (newPoDataSecurity?.Count > 0)
                    {
                        await db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"PoDataSecurity\"");
                        db.PoDataSecurity.AddRange(newPoDataSecurity);
                    }
                    await db.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    throw new Exception($"Cannot update security data in database error message is [{ex.Message}]");
                }
            }
            else
                throw new Exception("There is no security data in PoData excel file to update!!");
        }
        public async Task UpdateData()
        {
            List<Domain.Entity.PoData> excelPoData = new();
            try
            {
                if (tables?.Count > 0)
                {
                    if (tables[0].Rows?.Count > 0)
                    {
                        for (int i = 0; i < tables[0].Rows.Count; i++)
                        {
                            var Row = tables[0].Rows[i];
                            Domain.Entity.PoData NewUser = new()
                            {
                                PoNumber = Row["PO Number"].ToString().Trim(),

                                Rep = Row["Rep"].ToString().Trim(),
                                User = Row["User"].ToString().Trim(),
                                Date = GetDateTime(Row["Date"]),
                                CustomerPO = Row["Customer PO"].ToString().Trim(),
                                EstimateNumber = Row["Estimate Number"].ToString().Trim(),
                                Name = Row["Name"].ToString().Trim(),
                                DueDate = GetDateTime(Row["Due Date"]),
                                ItemGroup = Row["Item Group"].ToString().Trim(),
                                Forwarder = Row["Forwarder"].ToString().Trim(),
                                IOR = Row["IOR"].ToString().Trim(),
                                ShipTo = Row["Ship To"].ToString().Trim(),
                                ShippingCarrier = Row["Shipping Carrier"].ToString().Trim(),
                                ContainerNumber = Row["Container Number"].ToString().Trim(),
                                ETAAtPort = Row["ETA at Port"].ToString().Trim(),

                            };
                            excelPoData.Add(NewUser);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("there are some errors during reading data from PoData excel.[" + ex.Message + "]");
            }
            if (excelPoData?.Count > 0)
            {
                if (excelPoData.Any(x => string.IsNullOrWhiteSpace(x.PoNumber)))
                    throw new Exception("Some of records do not have [PoNumber]. [PoNumber] is mandatory.");
                try
                {
                    using var db = new Context();
                    var dbPoData = await db.PoDatas.ToListAsync().ConfigureAwait(false);
                    (var archivePoData, var newPoData, var removeList) = RecognizeOldDataNeedTobeArchivedOrUpdateOrInsert(dbPoData, excelPoData);

                    if (archivePoData?.Count > 0)
                        db.PoDatasArchive.AddRange(archivePoData);
                    if (newPoData?.Count > 0)
                        db.PoDatas.AddRange(newPoData);
                    if (removeList?.Count > 0)
                        db.PoDatas.RemoveRange(removeList);
                    int c = db.ChangeTracker.Entries<Domain.Entity.PoData>().Count(x => x.State == EntityState.Unchanged);
                    await db.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    throw new Exception($"Cannot update data in database error message is [{ex.Message}] + [{ex.InnerException?.Message}]");
                }
            }
            else
                throw new Exception("There is no data in PoData excel file to update!!");

        }

        private (List<Domain.Entity.PoDataArchive> archiveList, List<Domain.Entity.PoData> newList, List<Domain.Entity.PoData> removeList) RecognizeOldDataNeedTobeArchivedOrUpdateOrInsert(List<Domain.Entity.PoData> dbData, List<Domain.Entity.PoData> excelData)
        {
            List<Domain.Entity.PoDataArchive> needTobeArchived = new();
            List<Domain.Entity.PoData> newList = new();
            List<Domain.Entity.PoData> removeList = null;
            DateTime CurrentDate = DateTime.Now;
            foreach (var dbItem in dbData)
            {
                var excelItem = excelData.Find(x => string.Equals(x.PoNumber?.Trim(), dbItem.PoNumber.Trim()));
                //اگر پیدا شد باید به روز شود
                if (excelItem is not null)
                {
                    dbItem.Rep = excelItem.Rep;
                    dbItem.User = excelItem.User;
                    dbItem.Date = excelItem.Date;
                    dbItem.CustomerPO = excelItem.CustomerPO;
                    dbItem.EstimateNumber = excelItem.EstimateNumber;
                    dbItem.Name = excelItem.Name;
                    dbItem.DueDate = excelItem.DueDate;
                    dbItem.ItemGroup = excelItem.ItemGroup;
                    dbItem.Forwarder = excelItem.Forwarder;
                    dbItem.IOR = excelItem.IOR;
                    dbItem.ShipTo = excelItem.ShipTo;
                    dbItem.ShippingCarrier = excelItem.ShippingCarrier;
                    dbItem.ContainerNumber = excelItem.ContainerNumber;
                    dbItem.ETAAtPort = excelItem.ETAAtPort;
                    //از لیست حذف می شود چون دیگر نیازی بهش نداریم
                    excelData.Remove(excelItem);
                }
                //اگر پیدا نشد باید حذف شود و به جدول بایگانی انتقال داده شود
                else
                {
                    needTobeArchived.Add(new Domain.Entity.PoDataArchive()
                    {
                        PoNumber = dbItem.PoNumber?.Trim(),
                        Note = dbItem.Note,
                        Rep = dbItem.Rep,
                        User = dbItem.User,
                        Date = dbItem.Date,
                        CustomerPO = dbItem.CustomerPO,
                        EstimateNumber = dbItem.EstimateNumber,
                        Name = dbItem.Name,
                        DueDate = dbItem.DueDate,
                        ItemGroup = dbItem.ItemGroup,
                        Forwarder = dbItem.Forwarder,
                        IOR = dbItem.IOR,
                        ShipTo = dbItem.ShipTo,
                        ShippingCarrier = dbItem.ShippingCarrier,
                        ContainerNumber = dbItem.ContainerNumber,
                        BillDate = dbItem.BillDate ?? CurrentDate,
                        BookingDate = dbItem.BookingDate,
                        ConfirmDate = dbItem.ConfirmDate,
                        DischargeStatus = dbItem.DischargeStatus,
                        DocumentsSendOutDate = dbItem.DocumentsSendOutDate,
                        EmptyDate = dbItem.EmptyDate,
                        ETA = dbItem.ETA,
                        ETAAtPort = dbItem.ETAAtPort,
                        ETD = dbItem.ETA,
                        FactoryBookingDate = dbItem.FactoryBookingDate,
                        FactoryContainerNumber = dbItem.FactoryContainerNumber,
                        FactoryStatus = dbItem.FactoryStatus,
                        ForwarderName = dbItem.ForwarderName,
                        GateIn = dbItem.GateIn,
                        GateOut = dbItem.GateOut,
                        PortOfDischarge = dbItem.PortOfDischarge,
                        Rate = dbItem.Rate,
                        ShippmentStatus = dbItem.ShippmentStatus,
                        StatusDate = dbItem.StatusDate
                    });
                }
            }
            //از لیست حذف می شود چون باید به جدول آرشیو منتقل شود و از این جدول حذف شود
            if (needTobeArchived?.Count > 0)
            {
                removeList = dbData.Where(x => needTobeArchived.Any(y => string.Equals(y.PoNumber, x.PoNumber))).ToList();
            }

            //تمام داده های باقی مانده به عنوان داده جدید باید وارد سیستم شود
            newList.AddRange(excelData);

            return (needTobeArchived, newList, removeList);
        }

    }
}
