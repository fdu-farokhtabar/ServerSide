using System;

namespace Domain.Entity
{
    public class Role
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        /// <summary>
        /// PageName[R,W,E,D],PageName[R,W,E,D]
        /// </summary>
        public string Pages { get; set; }
        /// <summary>
        /// KeyValue
        /// </summary>
        public string Buttons { get; set; }
    }
}
