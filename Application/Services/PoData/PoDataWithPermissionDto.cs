using System.Collections.Generic;

namespace Application.Services.PoData
{
    public class PoDataWithPermissionDto
    {
        public List<PoDataDto> Data { get; set; }
        public List<ColAccess> ColumnsHavePermission { get; set; }
    }
    public class ColAccess
    {
        public ColAccess()
        {

        }

        public ColAccess(string colName, bool writable)
        {
            ColName = colName;
            Writable = writable;
        }

        public string ColName { get; set; }
        public bool Writable { get; set; }
    }
}
