using Application.Data;
using Application.Services.UpdateDataByExcel.Helper;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Application.Services.UpdateDataByExcel
{
    using Domain.Entity;
    public class UpdateFilterByExcelService
    {
        public async Task Update(Stream stream)
        {
            if (stream is null)
                throw new Exception("Please upload correct excel file.");
            List<Filter> Filters = new();
            try
            {
                var Tables = UpdateByExcelHelper.ReadExcel(stream);
                if (Tables?.Count > 0 && Tables[0].Rows?.Count > 0)
                {
                    for (int i = 0; i < Tables[0].Rows.Count; i++)
                    {
                        var Row = Tables[0].Rows[i];
                        if (Filters.Find(x => x.Name.Equals(Row["Name"].ToString().Trim(), StringComparison.OrdinalIgnoreCase)) is null)
                        {
                            Filter NewFilter = new()
                            {
                                Id = Guid.NewGuid(),
                                Name = Row["Name"].ToString().Trim(),
                                Order = UpdateByExcelHelper.GetInt32WithDefaultZero(Row["Position"]),
                                Groups = UpdateByExcelHelper.ConvertStringWithbracketsToJsonArrayString(Row["Groups"].ToString().Trim()),
                                Tags = UpdateByExcelHelper.ConvertStringWithbracketsToJsonArrayString(Row["Tags"].ToString().Trim())
                            };
                            Filters.Add(NewFilter);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("there are some errors during reading data from excel.[" + ex.Message + "]");
            }
            await UpdateDatabase(Filters);
        }
        private async Task UpdateDatabase(List<Filter> Filters)
        {
            try
            {
                using var Db = new Context();
                using var Trans = await Db.Database.BeginTransactionAsync();
                try
                {
                    Db.Database.ExecuteSqlRaw("DELETE FROM \"Filter\"");
                    Db.Filters.AddRange(Filters);
                    await Db.SaveChangesAsync();
                    Trans.Commit();
                }
                catch
                {
                    Trans.Rollback();
                    throw new Exception("Cannot update database");
                }
            }
            catch
            {
                throw new Exception("Cannot connect To the database");
            }
        }
    }
}
