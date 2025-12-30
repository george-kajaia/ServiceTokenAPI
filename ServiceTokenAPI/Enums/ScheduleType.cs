using Microsoft.EntityFrameworkCore;

namespace ServiceTokenAPI.Enums
{
    public enum SchedulePeriodType: int
    {
        None = 0,
        Daily = 1,
        Weekly = 2,
        Monthly = 3,
        Yearly = 4
    }

    [Owned]
    public class ScheduleType
    {
        public SchedulePeriodType PeriodType { get; set; }
        public int PeriodNumber { get; set; }
    }
}
