using CsvHelper;
using Application.Data;
using Application.SeedWork;
using Application.Services.Account;
using Application.Services.UpdateDataByExcel.Helper;
using Domain.Entity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services.PoData
{
    public class PoDataService
    {
        private readonly IApplicationSettings appSettings;
        private readonly AuthorizationService Auth;
        public PoDataService(IApplicationSettings appSettings, List<string> userRoles)
        {
            this.appSettings = appSettings;
            Auth = new AuthorizationService(userRoles);
        }
        public async Task<PoDataWithPermissionDto> Get()
        {
            PoSecurityData colSecurity = await GetPoSecurity().ConfigureAwait(false);
            PoDataWithPermissionDto result = new()
            {
                Data = GetRowsWhichUserHasAccess(await GetPoDataDto().ConfigureAwait(false)),
                ColumnsHavePermission = GetColAccess(colSecurity)
            };
            if (result.Data?.Count > 0)
                GetColsWhichUserHasAccess(result.Data, colSecurity);
            return result;
        }
        public async Task<PoDataWithPermissionDto> GetArchive()
        {
            PoSecurityData colSecurity = await GetPoSecurity().ConfigureAwait(false);
            PoDataWithPermissionDto result = new()
            {
                Data = GetRowsWhichUserHasAccess(await GetPoDataDtoByArchiveData().ConfigureAwait(false)),
                ColumnsHavePermission = GetColAccess(colSecurity)
            };
            if (result.Data?.Count > 0)
            {
                GetColsWhichUserHasAccess(result.Data, colSecurity);
                //همه دسترسی های نوشتن را غیر فعال می کنیم تا فقط برای نمایش استفاده شود
                foreach (var cPer in result.ColumnsHavePermission)
                    cPer.Writable = false;
            }
            return result;
        }
        public async Task<PoSaveDataResultDto> SaveData(List<Domain.Entity.PoData> data)
        {
            PoSaveDataResultDto result = null;
            List<Domain.Entity.PoData> dataMustBeMovedToArchive = new();
            using var db = new Context();
            if (data?.Count > 0)
            {
                PoSecurityData colSecurity = await GetPoSecurity().ConfigureAwait(false);
                foreach (var item in data)
                {
                    if (item.FactoryStatus.HasValue && item.FactoryStatus == FactoryStatus.None)
                        item.FactoryStatus = null;

                    if (item.ForwarderName.HasValue && item.ForwarderName == ForwarderName.None)
                        item.ForwarderName = null;

                    if (item.DischargeStatus.HasValue && item.DischargeStatus == DischargeStatus.None)
                        item.DischargeStatus = null;

                    if (item.ShippmentStatus.HasValue && item.ShippmentStatus == ShippmentStatus.None)
                        item.ShippmentStatus = null;
                }
                var dbData = await db.PoDatas.Where(x => data.Select(x => x.PoNumber).ToList().Contains(x.PoNumber)).ToListAsync();
                if (dbData?.Count > 0)
                {
                    dbData = dbData.Where(x => x.BillDate is null).ToList();
                    if (dbData?.Count > 0)
                    {
                        result = new PoSaveDataResultDto
                        {
                            Results = new List<PoSaveDataOutput>()
                        };
                        foreach (var item in data)
                        {
                            string message = "";
                            bool FactoryStatusNeedsToHaveReadyToGO = false;
                            var currentDate = DateTime.Now;
                            var dbItem = dbData.FirstOrDefault(x => string.Equals(x.PoNumber, item.PoNumber, StringComparison.OrdinalIgnoreCase));
                            //یعنی احتملا رکورد آرشیو رو می خواسته دوباره ثبت کند
                            if (dbItem is null)
                                continue;
                            //دقت شود باید آخرین وضعیت مربوط به
                            //ForwarderName
                            //ShippmentStatus
                            //را که در زمان خواندن است بررسی کرد نه وضعیت جدیدی که کاربر وارد کرده است
                            if (HasUserPermissionToThisRow(dbItem.Rep, dbItem.ForwarderName, dbItem.PoNumber, item.ShipTo, dbItem.ShippmentStatus))
                            {
                                if (colSecurity.FactoryStatus.Writable)
                                {
                                    //اگر وضعیت به حالت بوک بود دیگر نباید تغییر کند
                                    if (!dbItem.FactoryStatus.HasValue || dbItem.FactoryStatus != FactoryStatus.BookedWithForwarder)
                                    {
                                        //یعنی قبلا براش وضعیت رو مشخص کردیم
                                        if (item.FactoryStatus.HasValue)
                                        {
                                            if (dbItem.FactoryStatus != item.FactoryStatus)
                                            {
                                                dbItem.StatusDate = currentDate;

                                                if (item.FactoryStatus == FactoryStatus.BookedWithForwarder)
                                                {
                                                    dbItem.BookingDate = currentDate;
                                                    dbItem.FactoryStatus = item.FactoryStatus;
                                                }
                                                else
                                                {
                                                    //گزینه Ready To Go زمانی بتونی   ثبت کنه که تاریخ فعلی باید  حداقل چهارده روز قبل از etd باشد یعنی اگر 15 روز بود نمی تونه ولی اگر چهارده روز یا سیزده روز بود می تونه
                                                    if (item.FactoryStatus == FactoryStatus.ReadyToGo)
                                                    {
                                                        if (!(colSecurity.ETD.Writable && item.ETD.HasValue && (item.ETD.Value.Subtract(currentDate).TotalDays <= 14)))
                                                        {
                                                            message = "Cannot Accept FactoryStatus As a ReadyToGo because of ETD Date";
                                                        }
                                                        else
                                                        {
                                                            dbItem.FactoryStatus = item.FactoryStatus;
                                                            FactoryStatusNeedsToHaveReadyToGO = true;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        dbItem.FactoryStatus = item.FactoryStatus;
                                                    }
                                                    dbItem.BookingDate = null;
                                                }
                                            }
                                        }
                                        //یعنی وضعیت رو نال رد کردیم
                                        else
                                        {
                                            //برای اینکه هر دفعه با حالت نال دوباره تاریخ مجدد ثبت نکند
                                            if (dbItem.FactoryStatus is not null)
                                                dbItem.StatusDate = currentDate;
                                            dbItem.BookingDate = null;
                                            //dbItem.FactoryStatus = item.FactoryStatus;
                                            dbItem.FactoryStatus = null;
                                        }
                                    }
                                }

                                if (colSecurity.FactoryContainerNumber.Writable)
                                    dbItem.FactoryContainerNumber = item.FactoryContainerNumber;
                                if (colSecurity.DocumentsSendOutDate.Writable)
                                    dbItem.DocumentsSendOutDate = item.DocumentsSendOutDate;
                                if (colSecurity.ForwarderName.Writable)
                                    dbItem.ForwarderName = item.ForwarderName;
                                if (colSecurity.FactoryBookingDate.Writable)
                                    dbItem.FactoryBookingDate = item.FactoryBookingDate;

                                //در صورتی که ریت مجوز نوشتن نداشت و یانکه وضعیت برابر 0 بود و امکان نوشتن مجوز هم نبود و یا اینکه مجوز تغییر وضعیت بود ولی هم در پایگاه داده و داده ارسالی هر دو 0 بود
                                if (!(
                                    !colSecurity.Rate.Writable ||
                                    (dbItem.ShippmentStatus == 0 && !colSecurity.ShippmentStatus.Writable) ||
                                    (dbItem.ShippmentStatus == 0 && item.ShippmentStatus == 0 && colSecurity.ShippmentStatus.Writable)
                                    ))
                                {
                                    dbItem.Rate = item.Rate;
                                }
                                if (colSecurity.ETD.Writable)
                                {
                                    dbItem.ETD = item.ETD;
                                    if (item.ETD.HasValue && item.ETD.Value.Subtract(currentDate).TotalDays <= 14)
                                        FactoryStatusNeedsToHaveReadyToGO = true;
                                }
                                if (colSecurity.ETA.Writable)
                                    dbItem.ETA = item.ETA;
                                if (colSecurity.PortOfDischarge.Writable)
                                    dbItem.PortOfDischarge = item.PortOfDischarge;
                                if (colSecurity.DischargeStatus.Writable)
                                    dbItem.DischargeStatus = item.DischargeStatus;
                                if (colSecurity.ShippmentStatus.Writable)
                                {
                                    //یعنی قبلا چیزی برای وضعیت ترانزیت مشخص نشده بود
                                    if (!dbItem.ShippmentStatus.HasValue)
                                    {
                                        //اگر قبلا چیزی وارد نشده بود و حالا نوع انتقال رو در حالت پذیرفتن زده 
                                        if (item.ShippmentStatus.HasValue && item.ShippmentStatus == ShippmentStatus.PleaseAccept)
                                            dbItem.ConfirmDate = currentDate;
                                        else
                                            dbItem.ConfirmDate = null;
                                    }
                                    //وضعیت ترازیت قبلا مقدار داشته
                                    else
                                    {
                                        //وضعیت ترانزیت رو کاربر مشخص نکرده
                                        if (!item.ShippmentStatus.HasValue)
                                            dbItem.ConfirmDate = null;
                                        //وضعیت را کاربر مشخص کرده و حالت پذیرفتن نیست
                                        else if (item.ShippmentStatus != ShippmentStatus.PleaseAccept)
                                            dbItem.ConfirmDate = null;
                                        //وضعیت را کاربر مشخص کرده و حالت پذیرفتن است و  در پایگاه داده هم قبلا وضعیت پذیرفتن ثبت نشده است
                                        else if (dbItem.ShippmentStatus != ShippmentStatus.PleaseAccept)
                                            dbItem.ConfirmDate = currentDate;
                                    }
                                    dbItem.ShippmentStatus = item.ShippmentStatus;
                                }

                                if (colSecurity.GateIn.Writable)
                                    dbItem.GateIn = item.GateIn;
                                if (colSecurity.EmptyDate.Writable)
                                    dbItem.EmptyDate = item.EmptyDate;
                                if (colSecurity.GateOut.Writable)
                                    dbItem.GateOut = item.GateOut;
                                if (colSecurity.BillDate.Writable)
                                {
                                    dbItem.BillDate = item.BillDate;
                                    if (dbItem.BillDate is not null)
                                        dataMustBeMovedToArchive.Add(dbItem);
                                }
                                if (colSecurity.Note.Writable)
                                    dbItem.Note = item.Note;

                                result.Results.Add(new PoSaveDataOutput(item.PoNumber, item.ConfirmDate, item.StatusDate, item.BookingDate, message, FactoryStatusNeedsToHaveReadyToGO, item.Rate));
                            }
                            else
                                throw new Exception($"Cannot save data. Do not have permission to update the record with Po Number = {dbItem.PoNumber}");
                        }
                    }
                    else
                        throw new Exception("There is no data to save!");
                }
                else
                    throw new Exception("Cannot save data. There is no such data in the database!");
            }
            if (dataMustBeMovedToArchive.Count > 0)
            {
                foreach (var item in dataMustBeMovedToArchive)
                {
                    db.PoDatas.Remove(item);
                    Domain.Entity.PoDataArchive poDataArchive = new()
                    {
                        BillDate = item.BillDate,
                        BookingDate = item.BookingDate,
                        ConfirmDate = item.ConfirmDate,
                        StatusDate = item.StatusDate,
                        ContainerNumber = item.ContainerNumber,
                        CustomerPO = item.CustomerPO,
                        Date = item.Date,
                        DischargeStatus = item.DischargeStatus,
                        DocumentsSendOutDate = item.DocumentsSendOutDate,
                        DueDate = item.DueDate,
                        EmptyDate = item.EmptyDate,
                        EstimateNumber = item.EstimateNumber,
                        ETA = item.ETA,
                        ETAAtPort = item.ETAAtPort,
                        ETD = item.ETA,
                        FactoryBookingDate = item.FactoryBookingDate,
                        FactoryContainerNumber = item.FactoryContainerNumber,
                        FactoryStatus = item.FactoryStatus,
                        Forwarder = item.Forwarder,
                        ForwarderName = item.ForwarderName,
                        GateIn = item.GateIn,
                        GateOut = item.GateOut,
                        IOR = item.IOR,
                        ItemGroup = item.ItemGroup,
                        Name = item.Name,
                        Note = item.Note,
                        PoNumber = item.PoNumber,
                        PortOfDischarge = item.PortOfDischarge,
                        Rate = item.Rate,
                        Rep = item.Rep,
                        ShippingCarrier = item.ShippingCarrier,
                        ShippmentStatus = item.ShippmentStatus,
                        ShipTo = item.ShipTo,
                        User = item.User
                    };
                    db.PoDatasArchive.Add(poDataArchive);
                }
            }
            await db.SaveChangesAsync();
            return result;
        }

        public async Task<string> GetAsAnExcel(IApplicationSettings appSettings)
        {
            PoDataWithPermissionDto data = await Get().ConfigureAwait(false);
            DateTime Current = DateTime.Now;
            Random r = new();
            int rand = r.Next(int.MaxValue);
            string FileName = "PoData_" + rand + "_" + Current.Year.ToString("0000") + Current.Month.ToString("00") + Current.Day.ToString("00") + "-" +
                              Current.Hour.ToString("00") + Current.Minute.ToString("00") + Current.Second.ToString("00") + Current.Millisecond.ToString("000") + ".csv";
            string path = $"{appSettings.WwwRootPath}\\PoData\\{FileName}";
            using var writer = new StreamWriter(path);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            PoDataForExcelExportDtoMap.ColumnsHavePermission = data.ColumnsHavePermission;
            csv.Context.RegisterClassMap<PoDataForExcelExportDtoMap>();
            csv.WriteRecords(data.Data);
            return FileName;
        }
        private async Task<PoSecurityData> GetPoSecurity()
        {
            using var Db = new Context();
            var securityRows = await Db.PoDataSecurity.AsNoTracking().ToListAsync();
            PoSecurityData result = null;
            if (securityRows?.Count > 1)
            {
                var row = securityRows[0];
                var writePermissionRow = securityRows[1];
                result = new PoSecurityData()
                {
                    User = new ColPermission(row.User?.Trim(), writePermissionRow.User?.Trim(), Auth),
                    Date = new ColPermission(row.Date?.Trim(), writePermissionRow.Date?.Trim(), Auth),
                    CustomerPO = new ColPermission(row.CustomerPO?.Trim(), writePermissionRow.CustomerPO?.Trim(), Auth),
                    EstimateNumber = new ColPermission(row.EstimateNumber?.Trim(), writePermissionRow.EstimateNumber?.Trim(), Auth),
                    Name = new ColPermission(row.Name?.Trim(), writePermissionRow.Name?.Trim(), Auth),
                    PONumber = new ColPermission(row.PoNumber?.Trim(), writePermissionRow.PoNumber?.Trim(), Auth),
                    DueDate = new ColPermission(row.DueDate?.Trim(), writePermissionRow.DueDate?.Trim(), Auth),

                    ItemGroup = new ColPermission(row.ItemGroup?.Trim(), writePermissionRow.ItemGroup?.Trim(), Auth),
                    Forwarder = new ColPermission(row.Forwarder?.Trim(), writePermissionRow.Forwarder?.Trim(), Auth),
                    IOR = new ColPermission(row.IOR?.Trim(), writePermissionRow.IOR?.Trim(), Auth),
                    ShipTo = new ColPermission(row.ShipTo?.Trim(), writePermissionRow.ShipTo?.Trim(), Auth),
                    ShippingCarrier = new ColPermission(row.ShippingCarrier?.Trim(), writePermissionRow.ShippingCarrier?.Trim(), Auth),
                    ContainerNumber = new ColPermission(row.ContainerNumber?.Trim(), writePermissionRow.ContainerNumber?.Trim(), Auth),
                    ETAAtPort = new ColPermission(row.ETAAtPort?.Trim(), writePermissionRow.ETAAtPort?.Trim(), Auth),

                    FactoryStatus = new ColPermission(row.FactoryStatus?.Trim(), writePermissionRow.FactoryStatus?.Trim(), Auth),
                    StatusDate = new ColPermission(row.StatusDate?.Trim(), writePermissionRow.StatusDate?.Trim(), Auth),
                    FactoryContainerNumber = new ColPermission(row.FactoryContainerNumber?.Trim(), writePermissionRow.FactoryContainerNumber?.Trim(), Auth),
                    FactoryBookingDate = new ColPermission(row.FactoryBookingDate?.Trim(), writePermissionRow.FactoryBookingDate?.Trim(), Auth),
                    DocumentsSendOutDate = new ColPermission(row.DocumentsSendOutDate?.Trim(), writePermissionRow.DocumentsSendOutDate?.Trim(), Auth),

                    ForwarderName = new ColPermission(row.ForwarderName?.Trim(), writePermissionRow.ForwarderName?.Trim(), Auth),
                    BookingDate = new ColPermission(row.BookingDate?.Trim(), writePermissionRow.BookingDate?.Trim(), Auth),
                    Rate = new ColPermission(row.Rate?.Trim(), writePermissionRow.Rate?.Trim(), Auth),
                    ETD = new ColPermission(row.ETD?.Trim(), writePermissionRow.ETD?.Trim(), Auth),
                    ETA = new ColPermission(row.ETA?.Trim(), writePermissionRow.ETA?.Trim(), Auth),
                    PortOfDischarge = new ColPermission(row.PortOfDischarge?.Trim(), writePermissionRow.PortOfDischarge?.Trim(), Auth),
                    DischargeStatus = new ColPermission(row.DischargeStatus?.Trim(), writePermissionRow.DischargeStatus?.Trim(), Auth),
                    ShippmentStatus = new ColPermission(row.ShippmentStatus?.Trim(), writePermissionRow.ShippmentStatus?.Trim(), Auth),
                    ConfirmDate = new ColPermission(row.ConfirmDate?.Trim(), writePermissionRow.ConfirmDate?.Trim(), Auth),
                    GateIn = new ColPermission(row.GateIn?.Trim(), writePermissionRow.GateIn?.Trim(), Auth),
                    EmptyDate = new ColPermission(row.EmptyDate?.Trim(), writePermissionRow.EmptyDate?.Trim(), Auth),
                    GateOut = new ColPermission(row.GateOut?.Trim(), writePermissionRow.GateOut?.Trim(), Auth),
                    BillDate = new ColPermission(row.BillDate?.Trim(), writePermissionRow.BillDate?.Trim(), Auth),
                    Note = new ColPermission(row.Note?.Trim(), writePermissionRow.Note?.Trim(), Auth)
                };
            }
            else
                throw new Exception("PoData cannot Load, because there is not any security data!");

            return result;
        }
        private List<ColAccess> GetColAccess(PoSecurityData colSecurity)
        {
            List<ColAccess> results = new();
            if (colSecurity.User.HasAccess)
                results.Add(new ColAccess("User", colSecurity.User.Writable));
            if (colSecurity.Date.HasAccess)
                results.Add(new ColAccess("Date", colSecurity.Date.Writable));
            if (colSecurity.CustomerPO.HasAccess)
                results.Add(new ColAccess("Customer PO", colSecurity.CustomerPO.Writable));
            if (colSecurity.EstimateNumber.HasAccess)
                results.Add(new ColAccess("Estimate Number", colSecurity.EstimateNumber.Writable));
            if (colSecurity.Name.HasAccess)
                results.Add(new ColAccess("Name", colSecurity.Name.Writable));
            //if (ColSecurity.PONumber.HasAccess)
            //همیشه باید این فیلد باشد چون کلید است
            results.Add(new ColAccess("PO Number", false));
            if (colSecurity.DueDate.HasAccess)
                results.Add(new ColAccess("Due Date", colSecurity.DueDate.Writable));

            if (colSecurity.ItemGroup.HasAccess)
                results.Add(new ColAccess("Item Group", colSecurity.ItemGroup.Writable));
            if (colSecurity.Forwarder.HasAccess)
                results.Add(new ColAccess("Forwarder", colSecurity.Forwarder.Writable));
            if (colSecurity.IOR.HasAccess)
                results.Add(new ColAccess("IOR", colSecurity.IOR.Writable));
            if (colSecurity.ShipTo.HasAccess)
                results.Add(new ColAccess("Ship To", colSecurity.ShipTo.Writable));
            if (colSecurity.ShippingCarrier.HasAccess)
                results.Add(new ColAccess("Shipping Carrier", colSecurity.ShippingCarrier.Writable));
            if (colSecurity.ContainerNumber.HasAccess)
                results.Add(new ColAccess("Container Number", colSecurity.ContainerNumber.Writable));
            if (colSecurity.ETAAtPort.HasAccess)
                results.Add(new ColAccess("ETA at Port", colSecurity.ETAAtPort.Writable));

            if (colSecurity.FactoryStatus.HasAccess)
                results.Add(new ColAccess("Factory Status", colSecurity.FactoryStatus.Writable));
            if (colSecurity.StatusDate.HasAccess)
                results.Add(new ColAccess("Status Date", colSecurity.StatusDate.Writable));
            if (colSecurity.FactoryContainerNumber.HasAccess)
                results.Add(new ColAccess("Factory Container Number", colSecurity.FactoryContainerNumber.Writable));
            if (colSecurity.FactoryBookingDate.HasAccess)
                results.Add(new ColAccess("Factory Booking Date", colSecurity.FactoryBookingDate.Writable));
            if (colSecurity.DocumentsSendOutDate.HasAccess)
                results.Add(new ColAccess("Doc Send Out Date", colSecurity.DocumentsSendOutDate.Writable));

            if (colSecurity.ForwarderName.HasAccess)
                results.Add(new ColAccess("Forwarder Name", colSecurity.ForwarderName.Writable));

            if (colSecurity.BookingDate.HasAccess)
                results.Add(new ColAccess("Booking Date", colSecurity.BookingDate.Writable));
            if (colSecurity.Rate.HasAccess)
                results.Add(new ColAccess("Rate", colSecurity.Rate.Writable));
            if (colSecurity.ETD.HasAccess)
                results.Add(new ColAccess("ETD", colSecurity.ETD.Writable));
            if (colSecurity.ETA.HasAccess)
                results.Add(new ColAccess("ETA", colSecurity.ETA.Writable));
            if (colSecurity.PortOfDischarge.HasAccess)
                results.Add(new ColAccess("Port Of Discharge", colSecurity.PortOfDischarge.Writable));
            if (colSecurity.DischargeStatus.HasAccess)
                results.Add(new ColAccess("Discharge Status", colSecurity.DischargeStatus.Writable));
            if (colSecurity.ShippmentStatus.HasAccess)
                results.Add(new ColAccess("Shippment Status", colSecurity.ShippmentStatus.Writable));
            if (colSecurity.ConfirmDate.HasAccess)
                results.Add(new ColAccess("Confirm date", colSecurity.ConfirmDate.Writable));

            if (colSecurity.GateIn.HasAccess)
                results.Add(new ColAccess("Gate In", colSecurity.GateIn.Writable));
            if (colSecurity.EmptyDate.HasAccess)
                results.Add(new ColAccess("Empty Date", colSecurity.EmptyDate.Writable));
            if (colSecurity.GateOut.HasAccess)
                results.Add(new ColAccess("Gate out", colSecurity.GateOut.Writable));
            if (colSecurity.BillDate.HasAccess)
                results.Add(new ColAccess("Bill Date", colSecurity.BillDate.Writable));
            if (colSecurity.Note.HasAccess)
                results.Add(new ColAccess("Note", colSecurity.Note.Writable));

            return results;
        }
        private async Task<List<PoDataDto>> GetPoDataDto()
        {
            List<PoDataDto> results = null;
            using var db = new Context();
            var models = await db.PoDatas.AsNoTracking().Where(x => x.BillDate == null).ToListAsync().ConfigureAwait(false);
            if (models?.Count > 0)
            {
                results = new List<PoDataDto>();
                foreach (var model in models)
                    results.Add(MapToDto(model));
            }
            return results;
        }
        private async Task<List<PoDataDto>> GetPoDataDtoByArchiveData()
        {
            List<PoDataDto> results = null;
            using var db = new Context();
            var models = await db.PoDatasArchive.AsNoTracking().Where(x => x.BillDate != null).ToListAsync().ConfigureAwait(false);
            if (models?.Count > 0)
            {
                foreach (var model in models)
                    results.Add(MapToDto(model));
            }
            return results;
        }
        private PoDataDto MapToDto(Domain.Entity.PoData model)
        {
            return new PoDataDto()
            {
                User = model.User?.Trim(),
                Date = UpdateByExcelHelper.DateTimeToDateString(model.Date),
                CustomerPO = model.CustomerPO?.Trim(),
                EstimateNumber = model.EstimateNumber?.Trim(),
                Name = model.Name?.Trim(),
                PONumber = model.PoNumber.Trim(),
                DueDate = UpdateByExcelHelper.DateTimeToDateString(model.DueDate),

                ItemGroup = model.ItemGroup?.Trim(),
                Forwarder = model.Forwarder?.Trim(),
                IOR = model.IOR?.Trim(),
                /**
                 *به دلیل اینکه در مراحل دیگر به آن نیاز داریم فعلا به آن دسترسی می دهیم
                 */
                ShipTo = model.ShipTo?.Trim(),
                ShippingCarrier = model.ShippingCarrier?.Trim(),
                ContainerNumber = model.ContainerNumber?.Trim(),
                ETAAtPort = model.ETAAtPort?.Trim(),
                /**
                 * این فیلد خودش سطح دسترسی سطر مربوطه است و همیشه به آن نیاز است
                 **/
                Rep = model.Rep?.Trim(),

                FactoryStatus = model.FactoryStatus,
                StatusDate = model.StatusDate,
                FactoryContainerNumber = model.FactoryContainerNumber,
                FactoryBookingDate = model.FactoryBookingDate,
                DocumentsSendOutDate = model.DocumentsSendOutDate,
                /**
                 *به دلیل اینکه در مراحل دیگر به آن نیاز داریم یرای سطح دسترسی فعلا به آن دسترسی می دهیم
                */
                ForwarderName = model.ForwarderName,

                BookingDate = model.BookingDate,
                Rate = model.Rate,
                ETD = model.ETD,
                FactoryStatusNeedsToHaveReadyToGO = model.ETD.HasValue && (model.ETD.Value.Subtract(DateTime.Now).TotalDays <= 14),
                ETA = model.ETA,
                PortOfDischarge = model.PortOfDischarge,
                DischargeStatus = model.DischargeStatus,

                /**
                 *به دلیل اینکه در مراحل دیگر به آن نیاز داریم یرای سطح دسترسی فعلا به آن دسترسی می دهیم
                */
                ShippmentStatus = model.ShippmentStatus,

                ConfirmDate = model.ConfirmDate,

                GateIn = model.GateIn,
                EmptyDate = model.EmptyDate,
                GateOut = model.GateOut,
                BillDate = model.BillDate,
                Note = model.Note
            };
        }
        private PoDataDto MapToDto(Domain.Entity.PoDataArchive model)
        {
            return new PoDataDto()
            {
                User = model.User?.Trim(),
                Date = UpdateByExcelHelper.DateTimeToDateString(model.Date),
                CustomerPO = model.CustomerPO?.Trim(),
                EstimateNumber = model.EstimateNumber?.Trim(),
                Name = model.Name?.Trim(),
                PONumber = model.PoNumber.Trim(),
                DueDate = UpdateByExcelHelper.DateTimeToDateString(model.DueDate),

                ItemGroup = model.ItemGroup?.Trim(),
                Forwarder = model.Forwarder?.Trim(),
                IOR = model.IOR?.Trim(),
                /**
                 *به دلیل اینکه در مراحل دیگر به آن نیاز داریم فعلا به آن دسترسی می دهیم
                 */
                ShipTo = model.ShipTo?.Trim(),
                ShippingCarrier = model.ShippingCarrier?.Trim(),
                ContainerNumber = model.ContainerNumber?.Trim(),
                ETAAtPort = model.ETAAtPort?.Trim(),
                /**
                 * این فیلد خودش سطح دسترسی سطر مربوطه است و همیشه به آن نیاز است
                 **/
                Rep = model.Rep?.Trim(),

                FactoryStatus = model.FactoryStatus,
                StatusDate = model.StatusDate,
                FactoryContainerNumber = model.FactoryContainerNumber,
                FactoryBookingDate = model.FactoryBookingDate,
                DocumentsSendOutDate = model.DocumentsSendOutDate,
                /**
                 *به دلیل اینکه در مراحل دیگر به آن نیاز داریم یرای سطح دسترسی فعلا به آن دسترسی می دهیم
                */
                ForwarderName = model.ForwarderName,

                BookingDate = model.BookingDate,
                Rate = model.Rate,
                ETD = model.ETD,
                FactoryStatusNeedsToHaveReadyToGO = model.ETD.HasValue && (model.ETD.Value.Subtract(DateTime.Now).TotalDays <= 14),
                ETA = model.ETA,
                PortOfDischarge = model.PortOfDischarge,
                DischargeStatus = model.DischargeStatus,

                /**
                 *به دلیل اینکه در مراحل دیگر به آن نیاز داریم یرای سطح دسترسی فعلا به آن دسترسی می دهیم
                */
                ShippmentStatus = model.ShippmentStatus,

                ConfirmDate = model.ConfirmDate,

                GateIn = model.GateIn,
                EmptyDate = model.EmptyDate,
                GateOut = model.GateOut,
                BillDate = model.BillDate,
                Note = model.Note
            };
        }
        /// <summary>
        /// این متد مشخص می کند که کاربر به کدام سطر دسترسی دارد و به کدام ندارد
        /// </summary>
        /// <param name="pOData"></param>
        private List<PoDataDto> GetRowsWhichUserHasAccess(List<PoDataDto> rows)
        {
            if (rows?.Count > 0)
            {
                List<PoDataDto> authorizedRows = new();
                foreach (var row in rows)
                {
                    if (HasUserPermissionToThisRow(row.Rep, row.ForwarderName, row.PONumber, row.ShipTo, row.ShippmentStatus))
                        authorizedRows.Add(row);
                }
                if (authorizedRows.Count > 0)
                    return authorizedRows;
            }
            return null;
        }
        private bool HasUserPermissionToThisRow(string Rep, ForwarderName? ForwarderName, string PONumber, string ShipTo, ShippmentStatus? ShippmentStatus)
        {
            //به دلیل اینکه در زمان چک کردن نقش تابع نقش خالی را مجوز می دهد به همین خاطر یک چیزی داخلش گذاشتم که این حالت پیش نیاد
            string PONumberRole = "[]";
            if (PONumber.StartsWith("LY", StringComparison.OrdinalIgnoreCase) ||
                PONumber.StartsWith("KF", StringComparison.OrdinalIgnoreCase) ||
                PONumber.StartsWith("MH", StringComparison.OrdinalIgnoreCase) ||
                PONumber.StartsWith("DB", StringComparison.OrdinalIgnoreCase))
            {
                PONumberRole = "[\"" + PONumber[0..2] + " Factory\"]";
            }
            //به دلیل اینکه در زمان چک کردن نقش تابع نقش خالی را مجوز می دهد به همین خاطر یک چیزی داخلش گذاشتم که این حالت پیش نیاد
            string ShipToRole = "[]";
            if (!string.IsNullOrWhiteSpace(ShipTo))
            {
                ShipToRole = "[";
                if (ShipTo.StartsWith("KIAN USA", StringComparison.OrdinalIgnoreCase))
                    ShipToRole += "\"Harmun_Trucking\",\"KIAN_Employee_WH\"";
                if (ShipTo.StartsWith("Sacramento WH", StringComparison.OrdinalIgnoreCase) && ShippmentStatus.HasValue && ShippmentStatus == Domain.Entity.ShippmentStatus.PleaseAccept)
                {
                    ShipToRole += (ShipToRole == "[") ? "" : ",";
                    ShipToRole += "\"Check_ETA\"";
                }
                ShipToRole += "]";
            }

            if (Auth.HasUserPermissionToUseData(Rep) ||
                HasUserPermissionByCheckForwarderNameToThisRow(ForwarderName) || Auth.HasUserPermissionToUseData(PONumberRole) || Auth.HasUserPermissionToUseData(ShipToRole))
            {
                return true;
            }
            return false;
        }
        private bool HasUserPermissionByCheckForwarderNameToThisRow(ForwarderName? ForwarderName)
        {
            //به دلیل اینکه در زمان چک کردن نقش تابع نقش خالی را مجوز می دهد به همین خاطر یک چیزی داخلش گذاشتم که این حالت پیش نیاد
            string ForwarderNameRole = "[]";
            if (ForwarderName.HasValue)
                ForwarderNameRole = "[\"" + Enum.GetName(typeof(ForwarderName), ForwarderName) + "\"]";

            return Auth.HasUserPermissionToUseData(ForwarderNameRole);
        }
        /// <summary>
        /// بررسی می کند ستونهایی که دسترسی نداشته باشد نال می شود
        /// دقت شود این متد را بعد از سطح دسترسی به سطرها صدا بزنید
        /// </summary>
        /// <param name="rows"></param>
        /// <param name="colSecurity"></param>
        /// <returns></returns>
        private void GetColsWhichUserHasAccess(List<PoDataDto> rows, PoSecurityData colSecurity)
        {
            foreach (var row in rows)
            {
                row.User = colSecurity.User.HasAccess ? row.User : null;
                row.Date = colSecurity.Date.HasAccess ? row.Date : null;
                row.CustomerPO = colSecurity.CustomerPO.HasAccess ? row.CustomerPO : null;
                row.EstimateNumber = colSecurity.EstimateNumber.HasAccess ? row.EstimateNumber : null;
                row.Name = colSecurity.Name.HasAccess ? row.Name : null;
                /**
                 * دسترسی 
                 * PoNumber
                 * در نظر گرفته نمی شود چو کلید است وهمیشه به آن نیاز داریم
                 */
                row.PONumber = row.PONumber.Trim();
                row.DueDate = colSecurity.DueDate.HasAccess ? row.DueDate : null;

                row.ItemGroup = colSecurity.ItemGroup.HasAccess ? row.ItemGroup : null;
                row.Forwarder = colSecurity.Forwarder.HasAccess ? row.Forwarder : null;
                row.IOR = colSecurity.IOR.HasAccess ? row.IOR : null;

                row.ShipTo = colSecurity.ShipTo.HasAccess ? row.ShipTo : null;
                row.ShippingCarrier = colSecurity.ShippingCarrier.HasAccess ? row.ShippingCarrier : null;
                row.ContainerNumber = colSecurity.ContainerNumber.HasAccess ? row.ContainerNumber : null;
                row.ETAAtPort = colSecurity.ContainerNumber.HasAccess ? row.ETAAtPort : null;
                /**
                 * این فیلد خودش سطح دسترسی سطر مربوطه است و همیشه به آن نیاز است
                 **/
                row.Rep = row.Rep?.Trim();

                row.FactoryStatus = colSecurity.FactoryStatus.HasAccess ? row.FactoryStatus : null;
                row.StatusDate = colSecurity.StatusDate.HasAccess ? row.StatusDate : null;
                row.FactoryContainerNumber = colSecurity.FactoryContainerNumber.HasAccess ? row.FactoryContainerNumber : null;
                row.FactoryBookingDate = colSecurity.FactoryBookingDate.HasAccess ? row.FactoryBookingDate : null;
                row.DocumentsSendOutDate = colSecurity.DocumentsSendOutDate.HasAccess ? row.DocumentsSendOutDate : null;

                row.ForwarderName = colSecurity.ForwarderName.HasAccess ? row.ForwarderName : null;
                row.BookingDate = colSecurity.BookingDate.HasAccess ? row.BookingDate : null;
                row.Rate = colSecurity.Rate.HasAccess ? row.Rate : null;
                row.ETD = colSecurity.ETD.HasAccess ? row.ETD : null;
                row.ETA = colSecurity.ETA.HasAccess ? row.ETA : null;
                row.PortOfDischarge = colSecurity.PortOfDischarge.HasAccess ? row.PortOfDischarge : null;
                row.DischargeStatus = colSecurity.DischargeStatus.HasAccess ? row.DischargeStatus : null;

                row.ShippmentStatus = colSecurity.ShippmentStatus.HasAccess ? row.ShippmentStatus : null;
                row.ConfirmDate = colSecurity.ConfirmDate.HasAccess ? row.ConfirmDate : null;

                row.GateIn = colSecurity.GateIn.HasAccess ? row.GateIn : null;
                row.EmptyDate = colSecurity.EmptyDate.HasAccess ? row.EmptyDate : null;
                row.GateOut = colSecurity.GateOut.HasAccess ? row.GateOut : null;
                row.BillDate = colSecurity.BillDate.HasAccess ? row.BillDate : null;
                row.Note = colSecurity.Note.HasAccess ? row.Note : null;
            }
        }




        ///// <summary>
        ///// بررسی دسترسی بعضی ستون ها که در مرحله اول نیاز داریم آن را برای دسترسی به سطرها
        ///// </summary>
        ///// <param name="pOData"></param>
        ///// <param name="ColSecurity"></param>
        //private void CheckPermissionSomeDataAfterCheckingRowPermissions(PoDataWithPermissionDto pOData, PoSecurityData ColSecurity)
        //{
        //    if (pOData.Data?.Count > 0)
        //    {
        //        foreach (var data in pOData.Data)
        //        {
        //            data.ForwarderName = ColSecurity.ForwarderName.HasAccess ? data.ForwarderName : null;
        //            data.ShippmentStatus = ColSecurity.ShippmentStatus.HasAccess ? data.ShippmentStatus : null;
        //            data.ShipTo = ColSecurity.ShipTo.HasAccess ? data.ShipTo : null;
        //        }
        //    }
        //}



        ///// <summary>
        ///// این متد مشخص می کند که کاربر به کدام سطر دسترسی دارد و به کدام ندارد
        ///// </summary>
        ///// <param name="pOData"></param>
        //private void GetRowsWhichHaveBeAccessedByUser(PoDataWithPermissionDto pOData)
        //{
        //    List<PoDataDto> AuthorizedRows = new();
        //    foreach (var data in pOData.Data)
        //    {
        //        if (HasUserPermissionToThisRow(data.Rep, data.ForwarderName, data.PONumber, data.ShipTo, data.ShippmentStatus))
        //            AuthorizedRows.Add(data);
        //    }
        //    if (AuthorizedRows.Count > 0)
        //        pOData.Data = AuthorizedRows;
        //    else
        //        pOData.Data = null;
        //}


        //public (PoDataWithPermissionDto, PoSecurityData) GetDataByExcel()
        //{
        //    PoDataWithPermissionDto Result = null;
        //    PoSecurityData ColSecurity = null;
        //    var Tables = GetExcelTables();
        //    try
        //    {
        //        if (Tables?.Count > 0 && Tables[0].Rows?.Count > 0)
        //        {
        //            ColSecurity = null; //GetPoSecurity();

        //            Result = new PoDataWithPermissionDto
        //            {
        //                Data = new List<PoDataDto>()
        //            };
        //            if (ColSecurity is not null)
        //            {
        //                Result.ColumnsHavePermission = new List<ColAccess>();
        //                if (ColSecurity.User.HasAccess)
        //                    Result.ColumnsHavePermission.Add(new ColAccess("User", ColSecurity.User.Writable));
        //                if (ColSecurity.Date.HasAccess)
        //                    Result.ColumnsHavePermission.Add(new ColAccess("Date", ColSecurity.Date.Writable));
        //                if (ColSecurity.CustomerPO.HasAccess)
        //                    Result.ColumnsHavePermission.Add(new ColAccess("Customer PO", ColSecurity.CustomerPO.Writable));
        //                if (ColSecurity.EstimateNumber.HasAccess)
        //                    Result.ColumnsHavePermission.Add(new ColAccess("Estimate Number", ColSecurity.EstimateNumber.Writable));
        //                if (ColSecurity.Name.HasAccess)
        //                    Result.ColumnsHavePermission.Add(new ColAccess("Name", ColSecurity.Name.Writable));
        //                //if (ColSecurity.PONumber.HasAccess)
        //                //همیشه باید این فیلد باشد چون کلید است
        //                Result.ColumnsHavePermission.Add(new ColAccess("PO Number", false));
        //                if (ColSecurity.DueDate.HasAccess)
        //                    Result.ColumnsHavePermission.Add(new ColAccess("Due Date", ColSecurity.DueDate.Writable));

        //                if (ColSecurity.ItemGroup.HasAccess)
        //                    Result.ColumnsHavePermission.Add(new ColAccess("Item Group", ColSecurity.ItemGroup.Writable));
        //                if (ColSecurity.Forwarder.HasAccess)
        //                    Result.ColumnsHavePermission.Add(new ColAccess("Forwarder", ColSecurity.Forwarder.Writable));
        //                if (ColSecurity.IOR.HasAccess)
        //                    Result.ColumnsHavePermission.Add(new ColAccess("IOR", ColSecurity.IOR.Writable));
        //                if (ColSecurity.ShipTo.HasAccess)
        //                    Result.ColumnsHavePermission.Add(new ColAccess("Ship To", ColSecurity.ShipTo.Writable));
        //                if (ColSecurity.ShippingCarrier.HasAccess)
        //                    Result.ColumnsHavePermission.Add(new ColAccess("Shipping Carrier", ColSecurity.ShippingCarrier.Writable));
        //                if (ColSecurity.ContainerNumber.HasAccess)
        //                    Result.ColumnsHavePermission.Add(new ColAccess("Container Number", ColSecurity.ContainerNumber.Writable));
        //                if (ColSecurity.ETAAtPort.HasAccess)
        //                    Result.ColumnsHavePermission.Add(new ColAccess("ETA at Port", ColSecurity.ETAAtPort.Writable));

        //                if (ColSecurity.FactoryStatus.HasAccess)
        //                    Result.ColumnsHavePermission.Add(new ColAccess("Factory Status", ColSecurity.FactoryStatus.Writable));
        //                if (ColSecurity.StatusDate.HasAccess)
        //                    Result.ColumnsHavePermission.Add(new ColAccess("Status Date", ColSecurity.StatusDate.Writable));
        //                if (ColSecurity.FactoryContainerNumber.HasAccess)
        //                    Result.ColumnsHavePermission.Add(new ColAccess("Factory Container Number", ColSecurity.FactoryContainerNumber.Writable));
        //                if (ColSecurity.FactoryBookingDate.HasAccess)
        //                    Result.ColumnsHavePermission.Add(new ColAccess("Factory Booking Date", ColSecurity.FactoryBookingDate.Writable));
        //                if (ColSecurity.DocumentsSendOutDate.HasAccess)
        //                    Result.ColumnsHavePermission.Add(new ColAccess("Doc Send Out Date", ColSecurity.DocumentsSendOutDate.Writable));

        //                if (ColSecurity.ForwarderName.HasAccess)
        //                    Result.ColumnsHavePermission.Add(new ColAccess("Forwarder Name", ColSecurity.ForwarderName.Writable));

        //                if (ColSecurity.BookingDate.HasAccess)
        //                    Result.ColumnsHavePermission.Add(new ColAccess("Booking Date", ColSecurity.BookingDate.Writable));
        //                if (ColSecurity.Rate.HasAccess)
        //                    Result.ColumnsHavePermission.Add(new ColAccess("Rate", ColSecurity.Rate.Writable));
        //                if (ColSecurity.ETD.HasAccess)
        //                    Result.ColumnsHavePermission.Add(new ColAccess("ETD", ColSecurity.ETD.Writable));
        //                if (ColSecurity.ETA.HasAccess)
        //                    Result.ColumnsHavePermission.Add(new ColAccess("ETA", ColSecurity.ETA.Writable));
        //                if (ColSecurity.PortOfDischarge.HasAccess)
        //                    Result.ColumnsHavePermission.Add(new ColAccess("Port Of Discharge", ColSecurity.PortOfDischarge.Writable));
        //                if (ColSecurity.DischargeStatus.HasAccess)
        //                    Result.ColumnsHavePermission.Add(new ColAccess("Discharge Status", ColSecurity.DischargeStatus.Writable));
        //                if (ColSecurity.ShippmentStatus.HasAccess)
        //                    Result.ColumnsHavePermission.Add(new ColAccess("Shippment Status", ColSecurity.ShippmentStatus.Writable));
        //                if (ColSecurity.ConfirmDate.HasAccess)
        //                    Result.ColumnsHavePermission.Add(new ColAccess("Confirm date", ColSecurity.ConfirmDate.Writable));

        //                if (ColSecurity.GateIn.HasAccess)
        //                    Result.ColumnsHavePermission.Add(new ColAccess("Gate In", ColSecurity.GateIn.Writable));
        //                if (ColSecurity.EmptyDate.HasAccess)
        //                    Result.ColumnsHavePermission.Add(new ColAccess("Empty Date", ColSecurity.EmptyDate.Writable));
        //                if (ColSecurity.GateOut.HasAccess)
        //                    Result.ColumnsHavePermission.Add(new ColAccess("Gate out", ColSecurity.GateOut.Writable));
        //                if (ColSecurity.BillDate.HasAccess)
        //                    Result.ColumnsHavePermission.Add(new ColAccess("Bill Date", ColSecurity.BillDate.Writable));
        //                if (ColSecurity.Note.HasAccess)
        //                    Result.ColumnsHavePermission.Add(new ColAccess("Note", ColSecurity.Note.Writable));
        //            }
        //            for (int i = 0; i < Tables[0].Rows.Count; i++)
        //            {
        //                var row = Tables[0].Rows[i];

        //                ////به دلیل اینکه در زمان چک کردن نقش تابع نقش خالی را مجوز می دهد به همین خاطر یک چیزی داخلش گذاشتم که این حالت پیش نیاد
        //                //string ForwarderNameRole = "[]";
        //                //if (string.IsNullOrWhiteSpace(row["Forwarder"].ToString()))
        //                //    ForwarderNameRole = "[\"" + row["Forwarder"].ToString().Trim() + "\"]";
        //                ////هم نقش های معمول را چک میکند هم فیلد فوروادر را به عنوان نقش چک می کند که ببینید به این سطر دسترسی دارد یا نه
        //                //if (Auth.HasUserPermissionToUseData(row["Rep"].ToString().Trim()) || Auth.HasUserPermissionToUseData(ForwarderNameRole))
        //                //{
        //                var rowData = new PoDataDto()
        //                {
        //                    User = ColSecurity.User.HasAccess ? row["User"].ToString().Trim() : null,
        //                    Date = ColSecurity.Date.HasAccess ? UpdateByExcelHelper.DateTimeToDateString(row["Date"]) : null,
        //                    CustomerPO = ColSecurity.CustomerPO.HasAccess ? row["Customer PO"].ToString().Trim() : null,
        //                    EstimateNumber = ColSecurity.EstimateNumber.HasAccess ? row["Estimate Number"].ToString().Trim() : null,
        //                    Name = ColSecurity.Name.HasAccess ? row["Name"].ToString().Trim() : null,
        //                    //PONumber = ColSecurity.PONumber.HasAccess ? row["PO Number"].ToString().Trim() : null,
        //                    PONumber = row["PO Number"].ToString().Trim(),
        //                    DueDate = ColSecurity.DueDate.HasAccess ? UpdateByExcelHelper.DateTimeToDateString(row["Due Date"]) : null,

        //                    ItemGroup = ColSecurity.ItemGroup.HasAccess ? row["Item Group"].ToString().Trim() : null,
        //                    Forwarder = ColSecurity.Forwarder.HasAccess ? row["Forwarder"].ToString().Trim() : null,
        //                    IOR = ColSecurity.IOR.HasAccess ? row["IOR"].ToString().Trim() : null,
        //                    //ShipTo = ColSecurity.ShipTo.HasAccess ? row["Ship To"].ToString().Trim() : null,
        //                    //فعلا دسترسی ستون رو بهش نمی دیم چو بهش نیاز داریم برای سطح دسترسی سطر ها
        //                    ShipTo = row["Ship To"].ToString().Trim(),
        //                    ShippingCarrier = ColSecurity.ShippingCarrier.HasAccess ? row["Shipping Carrier"].ToString().Trim() : null,
        //                    ContainerNumber = ColSecurity.ContainerNumber.HasAccess ? row["Container Number"].ToString().Trim() : null,
        //                    ETAAtPort = ColSecurity.ContainerNumber.HasAccess ? row["ETA at Port"].ToString().Trim() : null,
        //                    Rep = row["Rep"].ToString().Trim()
        //                };
        //                Result.Data.Add(rowData);
        //                //}
        //            }
        //        }
        //    }
        //    catch (Exception Ex)
        //    {
        //        throw new Exception("there are some errors during reading data from excel.");
        //    }
        //    return (Result, ColSecurity);
        //}
        //public async Task FillOutLiveDbDataToPoData(PoDataWithPermissionDto pOData, PoSecurityData ColSecurity)
        //{
        //    using var Db = new Context();
        //    var models = await Db.PoDatas.AsNoTracking().ToListAsync();
        //    FillDbColumnsBasedOnPermissions(pOData, models, ColSecurity);

        //    //if (models?.Count > 0 && pOData.Data?.Count > 0)
        //    //{
        //    //    foreach (var data in pOData.Data)
        //    //    {
        //    //        var model = models.FirstOrDefault(x => string.Equals(x.PoNumber, data.PONumber, StringComparison.OrdinalIgnoreCase));
        //    //        if (model is not null)
        //    //        {
        //    //            data.FactoryStatus = ColSecurity.FactoryStatus.HasAccess ? model.FactoryStatus : null;
        //    //            data.StatusDate = ColSecurity.StatusDate.HasAccess ? model.StatusDate : null;
        //    //            data.FactoryContainerNumber = ColSecurity.FactoryContainerNumber.HasAccess ? model.FactoryContainerNumber : null;
        //    //            data.FactoryBookingDate = ColSecurity.FactoryBookingDate.HasAccess ? model.FactoryBookingDate : null;
        //    //            data.DocumentsSendOutDate = ColSecurity.DocumentsSendOutDate.HasAccess ? model.DocumentsSendOutDate : null;


        //    //            //فعلا دسترسی ستون رو بهش نمی دیم چو بهش نیاز داریم برای سطح دسترسی سطر ها
        //    //            //data.ForwarderName = ColSecurity.ForwarderName.HasAccess ?  model.ForwarderName : null;
        //    //            data.ForwarderName = model.ForwarderName;
        //    //            data.BookingDate = ColSecurity.BookingDate.HasAccess ? model.BookingDate : null;
        //    //            data.Rate = ColSecurity.Rate.HasAccess ? model.Rate : null;
        //    //            data.ETD = ColSecurity.ETD.HasAccess ? model.ETD : null;
        //    //            data.FactoryStatusNeedsToHaveReadyToGO = data.ETD.HasValue && (data.ETD.Value.Subtract(DateTime.Now).TotalDays <= 14);
        //    //            data.ETA = ColSecurity.ETA.HasAccess ? model.ETA : null;
        //    //            data.PortOfDischarge = ColSecurity.PortOfDischarge.HasAccess ? model.PortOfDischarge : null;
        //    //            data.DischargeStatus = ColSecurity.DischargeStatus.HasAccess ? model.DischargeStatus : null;

        //    //            data.ShippmentStatus = ColSecurity.ShippmentStatus.HasAccess ? model.ShippmentStatus : null;
        //    //            data.ConfirmDate = ColSecurity.ConfirmDate.HasAccess ? model.ConfirmDate : null;

        //    //            data.GateIn = ColSecurity.GateIn.HasAccess ? model.GateIn : null;
        //    //            data.EmptyDate = ColSecurity.EmptyDate.HasAccess ? model.EmptyDate : null;
        //    //            data.GateOut = ColSecurity.GateOut.HasAccess ? model.GateOut : null;

        //    //            data.BillDate = ColSecurity.BillDate.HasAccess ? model.BillDate : null;
        //    //        }

        //    //    }
        //    //}
        //    GetRowsWhichHaveBeAccessedByUser(pOData);
        //    //if (pOData.Data?.Count > 0)
        //    //{
        //    //    foreach (var data in pOData.Data)
        //    //    {
        //    //        data.ForwarderName = ColSecurity.ForwarderName.HasAccess ? data.ForwarderName : null;
        //    //        data.ShipTo = ColSecurity.ShipTo.HasAccess ? data.ShipTo : null;
        //    //    }
        //    //}
        //    CheckPermissionSomeDataAfterCheckingRowPermissions(pOData, ColSecurity);
        //    pOData.Data = pOData.Data?.Where(x => x.BillDate is null).ToList();
        //}
        //public async Task FillOutArchiveDbDataToPoData(PoDataWithPermissionDto pOData, PoSecurityData ColSecurity)
        //{
        //    using var Db = new Context();
        //    var models = await Db.PoDatas.AsNoTracking().Where(x => x.BillDate != null).ToListAsync();
        //    FillDbColumnsBasedOnPermissions(pOData, models, ColSecurity);
        //    GetRowsWhichHaveBeAccessedByUser(pOData);
        //    CheckPermissionSomeDataAfterCheckingRowPermissions(pOData, ColSecurity);
        //    //به این دلیل دوباره این شرط را میزارم که داده های اکسلی که معادلی در پایگاه داده نداشته اند را با این شرط حذف کند
        //    pOData.Data = pOData.Data?.Where(x => x.BillDate is not null).ToList();
        //    //همه دسترسی های نوشتن را غیر فعال می کنیم تا فقط برای نمایش استفاده شود
        //    foreach (var cPer in pOData.ColumnsHavePermission)
        //    {
        //        cPer.Writable = false;
        //    }
        //}
        //private void FillDbColumnsBasedOnPermissions(PoDataWithPermissionDto pOData, List<Domain.Entity.PoData> models, PoSecurityData ColSecurity)
        //{
        //    if (models?.Count > 0 && pOData.Data?.Count > 0)
        //    {
        //        foreach (var data in pOData.Data)
        //        {
        //            var model = models.FirstOrDefault(x => string.Equals(x.PoNumber, data.PONumber, StringComparison.OrdinalIgnoreCase));
        //            if (model is not null)
        //            {
        //                data.FactoryStatus = ColSecurity.FactoryStatus.HasAccess ? model.FactoryStatus : null;
        //                data.StatusDate = ColSecurity.StatusDate.HasAccess ? model.StatusDate : null;
        //                data.FactoryContainerNumber = ColSecurity.FactoryContainerNumber.HasAccess ? model.FactoryContainerNumber : null;
        //                data.FactoryBookingDate = ColSecurity.FactoryBookingDate.HasAccess ? model.FactoryBookingDate : null;
        //                data.DocumentsSendOutDate = ColSecurity.DocumentsSendOutDate.HasAccess ? model.DocumentsSendOutDate : null;

        //                //فعلا دسترسی ستون رو بهش نمی دیم چو بهش نیاز داریم برای سطح دسترسی سطر ها
        //                //data.ForwarderName = ColSecurity.ForwarderName.HasAccess ?  model.ForwarderName : null;
        //                data.ForwarderName = model.ForwarderName;

        //                data.BookingDate = ColSecurity.BookingDate.HasAccess ? model.BookingDate : null;
        //                data.Rate = ColSecurity.Rate.HasAccess ? model.Rate : null;
        //                data.ETD = ColSecurity.ETD.HasAccess ? model.ETD : null;
        //                data.FactoryStatusNeedsToHaveReadyToGO = data.ETD.HasValue && (data.ETD.Value.Subtract(DateTime.Now).TotalDays <= 14);
        //                data.ETA = ColSecurity.ETA.HasAccess ? model.ETA : null;
        //                data.PortOfDischarge = ColSecurity.PortOfDischarge.HasAccess ? model.PortOfDischarge : null;
        //                data.DischargeStatus = ColSecurity.DischargeStatus.HasAccess ? model.DischargeStatus : null;

        //                //فعلا دسترسی ستون رو بهش نمی دیم چو بهش نیاز داریم برای سطح دسترسی سطر ها
        //                //data.ShippmentStatus = ColSecurity.ShippmentStatus.HasAccess ? model.ShippmentStatus : null;
        //                data.ShippmentStatus = model.ShippmentStatus;

        //                data.ConfirmDate = ColSecurity.ConfirmDate.HasAccess ? model.ConfirmDate : null;

        //                data.GateIn = ColSecurity.GateIn.HasAccess ? model.GateIn : null;
        //                data.EmptyDate = ColSecurity.EmptyDate.HasAccess ? model.EmptyDate : null;
        //                data.GateOut = ColSecurity.GateOut.HasAccess ? model.GateOut : null;

        //                data.BillDate = ColSecurity.BillDate.HasAccess ? model.BillDate : null;
        //                data.Note = ColSecurity.Note.HasAccess ? model.Note : null;
        //            }
        //        }
        //    }
        //}        


        //private DataTableCollection GetExcelTables()
        //{
        //    try
        //    {
        //        FileStream excelDataStream = new(appSettings.ImportPath, FileMode.Open, FileAccess.Read);
        //        if (excelDataStream is null)
        //            throw new Exception("I counld not find any excel files!");
        //        return UpdateByExcelHelper.ReadExcel(excelDataStream);
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception(ex.Message);
        //    }
        //}
    }
}
