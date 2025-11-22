namespace EWOS_MVC.Areas.AdminSystem.Controllers
{
    public class WeekSettingController
    {

        // generate minggu jika belum ada
        /*if (!await _context.WeeksSetting.AnyAsync(w => w.YearSettingId == getTahun.Id))
        {
            var weekStart = getTahun.StartDate;
            int weekNumber = 1;
            var akhirTahun = new DateTime(tahunSekarang, 12, 31, 23, 59, 59);

            while (weekStart <= akhirTahun)
            {
                var nextWeekDate = weekStart.AddDays(7);
                DateTime weekEnd = new DateTime(
                    nextWeekDate.Year, nextWeekDate.Month, nextWeekDate.Day,
                    6, 59, 59
                );
                if (weekEnd > akhirTahun)
                    weekEnd = akhirTahun;

                _context.WeeksSetting.Add(new WeeksSettingModel
                {
                    YearSettingId = getTahun.Id,
                    Week = weekNumber,
                    Month = weekStart.Month,
                    WorkingDays = 5.5m,
                    StartDate = weekStart,
                    EndDate = weekEnd
                });

                weekNumber++;
                weekStart = weekEnd.AddSeconds(1);
            }

            await _context.SaveChangesAsync();
        }*/

    }
}
