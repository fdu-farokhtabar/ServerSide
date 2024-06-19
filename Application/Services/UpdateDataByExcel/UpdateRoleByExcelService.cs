using Application.Data;
using Application.Services.UpdateDataByExcel.Helper;
using Domain.Entity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;

namespace Application.Services.UpdateDataByExcel
{
    public class UpdateRoleByExcelService
    {
        public async Task Update(Stream stream)
        {
            if (stream is null)
                throw new Exception("Please upload correct excel file.");
            List<Role> Roles = new();
            try
            {
                var Tables = UpdateByExcelHelper.ReadExcel(stream);
                if (Tables?.Count > 0 && Tables[0].Rows?.Count > 0)
                {
                    for (int i = 0; i < Tables[0].Rows.Count; i++)
                    {
                        var Row = Tables[0].Rows[i];
                        if (Roles.Find(x => x.Name.Equals(Row["Name"].ToString().Trim(), StringComparison.OrdinalIgnoreCase)) is null)
                        {
                            Role NewRole = new()
                            {
                                Id = Guid.NewGuid(),
                                Name = Row["Name"].ToString().Trim(),
                                Pages = CreatesPages(Tables[0].Columns, Row),
                                Buttons = UpdateByExcelHelper.CreateJsonKeyValue(Tables[0].Columns, Row, "Button"),
                            };
                            Roles.Add(NewRole);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("there are some errors during reading data from excel.[" + ex.Message + "]");
            }
            var AdminRole = Roles.Find(x => x.Name.Equals("admin", StringComparison.OrdinalIgnoreCase));
            if (AdminRole is null)
                Roles.Add(new Role() { Id = Guid.NewGuid(), Name = "Admin", Pages = null });
            await UpdateDatabase(Roles);
        }
        private async Task UpdateDatabase(List<Role> Roles)
        {
            try
            {
                using var Db = new Context();
                using var Trans = await Db.Database.BeginTransactionAsync();
                try
                {
                    Db.Database.ExecuteSqlRaw("DELETE FROM \"Role\"");
                    Db.Roles.AddRange(Roles);
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
        private string CreatesPages(DataColumnCollection Columns, DataRow Row)
        {
            string Pages = null;
            foreach (var Column in Columns)
            {
                string ColumnName = Column.ToString();
                if (ColumnName.StartsWith("Page", StringComparison.OrdinalIgnoreCase))
                {
                    int? PagePermission = UpdateByExcelHelper.GetInt32(Row[ColumnName]);
                    if (PagePermission.HasValue && PagePermission == 1)
                    {
                        Pages += $"{ColumnName.Replace("Page", "", StringComparison.OrdinalIgnoreCase).Trim()}[RWED],";
                    }
                }
            }
            if (!string.IsNullOrEmpty(Pages))
                Pages = Pages[0..^1];
            return Pages;
        }
    }
}
