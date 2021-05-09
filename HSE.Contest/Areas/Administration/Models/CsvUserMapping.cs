using TinyCsvParser.Mapping;

namespace HSE.Contest.Areas.Administration.Models
{
    public class CsvUserMapping : CsvMapping<CsvUser>
    {
        public CsvUserMapping()
        : base()
        {
            MapProperty(0, x => x.LastName);
            MapProperty(1, x => x.FirstName);
            MapProperty(2, x => x.Email);
            MapProperty(3, x => x.Password);
            MapProperty(4, x => x.GroupName);
        }
    }
}
