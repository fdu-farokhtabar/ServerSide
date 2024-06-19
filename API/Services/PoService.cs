using Grpc.Core;
using API.Helper;
using Application.SeedWork;
using Application.Services.PoData;
using Domain.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace API.Services
{
    [Authorize]
    public class PoService : PoSrv.PoSrvBase
    {
        private Application.Services.PoData.PoDataService service;
        private readonly ILogger<PoService> logger;
        private readonly IApplicationSettings applicationSettings;
        public PoService(IApplicationSettings applicationSettings, ILogger<PoService> logger)
        {
            this.logger = logger;
            this.applicationSettings = applicationSettings;
        }
        public override async Task<PoResponse> Get(PoGetRequest request, ServerCallContext context)
        {
            NewService(context);
            PoDataWithPermissionDto data;
            if (request.IsArchive)
                data = await service.GetArchive();
            else
                data = await service.Get();

            //(PoDataWithPermissionDto data, PoSecurityData Security) = service.GetDataByExcel();
            //if (request.IsArchive)
            //    await service.FillOutArchiveDbDataToPoData(data, Security);
            //else
            //    await service.FillOutLiveDbDataToPoData(data, Security);

            PoResponse result = new();
            if (data?.Data?.Count > 0)
                foreach (var item in data.Data)
                    result.ExcelData.Add(MapToPoData(item));
            if (data?.ColumnsHavePermission?.Count > 0)
            {
                foreach (var colPer in data.ColumnsHavePermission)
                {
                    result.ColumnsHavePermission.Add(new PoColAccess()
                    {
                        ColName = colPer.ColName,
                        IsWritable = colPer.Writable
                    });
                }
            }
            return await Task.FromResult(result);
        }
        public override async Task<PoExcelResponse> GetExcel(PoGetRequest request, ServerCallContext context)
        {
            NewService(context);
            return new PoExcelResponse()
            {
                Url = await service.GetAsAnExcel(applicationSettings)
            };

        }
        public override async Task<PoSaveResponse> Save(PoDataSaveRequest request, ServerCallContext context)
        {
            NewService(context);
            try
            {
                if (request is not null && request.Data?.Count > 0)
                {
                    List<Domain.Entity.PoData> entities = new();
                    foreach (var data in request.Data)
                        entities.Add(MapToPoDataEntity(data));

                    var Result = await service.SaveData(entities);
                    return await Task.FromResult(MapToPoSaveResult(false, "Data is successfully saved.", Result));
                }
                else
                    return await Task.FromResult(new PoSaveResponse() { IsError = false, Message = "There is not data to save!" });
            }
            catch (Exception Ex)
            {
                logger.LogError(Ex, $"ERROR IN: {typeof(PoService).FullName}.{nameof(Save)}");
                return await Task.FromResult(new PoSaveResponse() { IsError = true, Message = Ex.Message });
            }
        }
        private PoSaveResponse MapToPoSaveResult(bool IsError, string Message, PoSaveDataResultDto data)
        {
            var result = new PoSaveResponse()
            {
                IsError = IsError,
                Message = Message
            };
            if (data is not null)
            {
                if (data.Results?.Count > 0)
                {
                    foreach (var item in data.Results)
                    {
                        result.Results.Add(new PoSaveResult()
                        {
                            BookingDate = Tools.DateTimeToDateString(item.BookingDate),
                            ConfirmDate = Tools.DateTimeToDateString(item.ConfirmDate),
                            PoNumber = item.PoNumber,
                            StatusDate = Tools.DateTimeToDateString(item.StatusDate),
                            FactoryStatusNeedsToHaveReadyToGO = item.FactoryStatusNeedsToHaveReadyToGO,
                            Rate = item.Rate,
                            Message = item.Message
                        });
                    }
                }
            }
            return result;
        }
        private Domain.Entity.PoData MapToPoDataEntity(PoDataSave data)
        {
            try
            {
                return new Domain.Entity.PoData()
                {
                    PoNumber = data.PONumber,
                    FactoryStatus = (FactoryStatus?)data.FactoryStatus,
                    StatusDate = null,
                    FactoryContainerNumber = data.FactoryContainerNumber,
                    FactoryBookingDate = null,
                    DocumentsSendOutDate = Tools.DateStringToDateTime(data.DocumentsSendOutDate),

                    ForwarderName = (ForwarderName?)data.ForwarderName,
                    BookingDate = Tools.DateStringToDateTime(data.BookingDate),
                    Rate = data.Rate,
                    ETD = Tools.DateStringToDateTime(data.ETD),
                    ETA = Tools.DateStringToDateTime(data.ETA),
                    PortOfDischarge = data.PortOfDischarge,
                    DischargeStatus = (DischargeStatus?)data.DischargeStatus,
                    ShippmentStatus = (ShippmentStatus?)data.ShippmentStatus,
                    ConfirmDate = null,

                    GateIn = Tools.DateStringToDateTime(data.GateIn),
                    EmptyDate = Tools.DateStringToDateTime(data.EmptyDate),
                    GateOut = Tools.DateStringToDateTime(data.GateOut),
                    BillDate = Tools.DateStringToDateTime(data.BillDate),
                    Note = data.Note
                };
            }
            catch (Exception Ex)
            {
                logger.LogError(Ex, "Data is not valid! data is => {@data}", data);
                throw new Exception("Data is not valid!", Ex);
            }
        }
        private PoData MapToPoData(PoDataDto dataDto)
        {
            return new PoData()
            {
                User = Tools.NullStringToEmpty(dataDto.User),
                Date = Tools.NullStringToEmpty(dataDto.Date),
                CustomerPO = Tools.NullStringToEmpty(dataDto.CustomerPO),
                EstimateNumber = Tools.NullStringToEmpty(dataDto.EstimateNumber),
                Name = Tools.NullStringToEmpty(dataDto.Name),
                PONumber = Tools.NullStringToEmpty(dataDto.PONumber),
                DueDate = Tools.NullStringToEmpty(dataDto.DueDate),

                ItemGroup = Tools.NullStringToEmpty(dataDto.ItemGroup),
                Forwarder = Tools.NullStringToEmpty(dataDto.Forwarder),
                IOR = Tools.NullStringToEmpty(dataDto.IOR),
                ShipTo = Tools.NullStringToEmpty(dataDto.ShipTo),
                ShippingCarrier = Tools.NullStringToEmpty(dataDto.ShippingCarrier),
                ContainerNumber = Tools.NullStringToEmpty(dataDto.ContainerNumber),
                ETAAtPort = Tools.NullStringToEmpty(dataDto.ETAAtPort),

                FactoryStatus = (int?)dataDto.FactoryStatus,
                FactoryStatusNeedsToHaveReadyToGO = dataDto.FactoryStatusNeedsToHaveReadyToGO,
                StatusDate = Tools.DateTimeToDateString(dataDto.StatusDate),
                FactoryContainerNumber = Tools.NullStringToEmpty(dataDto.FactoryContainerNumber),
                FactoryBookingDate = Tools.DateTimeToDateString(dataDto.FactoryBookingDate),
                DocumentsSendOutDate = Tools.DateTimeToDateString(dataDto.DocumentsSendOutDate),

                ForwarderName = (int?)dataDto.ForwarderName,
                BookingDate = Tools.DateTimeToDateString(dataDto.BookingDate),
                Rate = dataDto.Rate,
                ETD = Tools.DateTimeToDateString(dataDto.ETD),
                ETA = Tools.DateTimeToDateString(dataDto.ETA),
                PortOfDischarge = Tools.NullStringToEmpty(dataDto.PortOfDischarge),
                DischargeStatus = (int?)dataDto.DischargeStatus,
                ShippmentStatus = (int?)dataDto.ShippmentStatus,
                ConfirmDate = Tools.DateTimeToDateString(dataDto.ConfirmDate),

                GateIn = Tools.DateTimeToDateString(dataDto.GateIn),
                EmptyDate = Tools.DateTimeToDateString(dataDto.EmptyDate),
                GateOut = Tools.DateTimeToDateString(dataDto.GateOut),
                BillDate = Tools.DateTimeToDateString(dataDto.BillDate),
                Note = Tools.NullStringToEmpty(dataDto.Note)
            };
        }
        private void NewService(ServerCallContext context)
        {
            service = new(applicationSettings, Tools.GetRoles(context));
        }
    }
}