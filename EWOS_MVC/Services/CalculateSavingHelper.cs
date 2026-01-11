using EWOS_MVC.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace EWOS_MVC.Services
{
    public class CalculateSavingHelper
    {
        private readonly AppDbContext _context;

        public CalculateSavingHelper(AppDbContext context)
        {
            _context = context;
        }


        public (decimal totalSaving, decimal fabricationTime)
            Calculate(ItemRequestModel item, MachineModel machine, YearsSettingModel year, int qty)
        {
            // Jika saving tidak dihitung → return 0
            if (!item.IsCalculateSaving)
                return (0m, 0m);

            decimal fabricationTime = (item.FabricationTime ?? 0m) * qty;
            decimal rawMaterialCost = 0m;
            decimal inhouseCost = 0m;
            decimal externalCost = (item.ExternalFabCost ?? 0m) * qty;

            switch (item.MachineCategoryId)
            {

                case 1: // CNC
                    rawMaterialCost =
                        (item.RawMaterials?.Price ?? 0m) * qty;

                    inhouseCost =
                        fabricationTime
                        * (year.ElectricalCost ?? 0m)
                        * machine.MachinePower;
                    break;

                case 2: // 3D Printing
                    rawMaterialCost =
                        ((item.Weight ?? 0m) / 1000m)
                        * (item.RawMaterials?.Price ?? 0m)
                        * qty;

                    inhouseCost =
                        fabricationTime
                        * (year.ElectricalCost ?? 0m)
                        * machine.MachinePower;
                    break;

                default:
                    return (0m, fabricationTime);
            }

            decimal totalSaving = externalCost - (rawMaterialCost + inhouseCost);

            return (totalSaving, fabricationTime);
        }

        public async Task<(decimal totalSaving, decimal totalFabricationTime)> CalculateAllRo(
    List<RepeatOrderModel> repeatOrders,
    YearsSettingModel year)
        {
            decimal grandTotalSaving = 0m;
            decimal grandFabricationTime = 0m;

            var machines = await _context.Machines.ToListAsync();

            foreach (var repeatOrder in repeatOrders)
            {
                var item = repeatOrder.ItemRequests;
                if (item == null) continue;

                var machine = machines
                    .FirstOrDefault(x => x.MachineCategoryId == item.MachineCategoryId);

                if (machine == null) continue;

                // qty sesuai row
                var qty = repeatOrder.QuantityReq;

                // hitung per row item
                var (saving, fabTime) = Calculate(item, machine, year, qty);

                grandTotalSaving += saving;
                grandFabricationTime += fabTime;
            }

            return (grandTotalSaving, grandFabricationTime);
        }


        // Hitung semua new request sekaligus
        public async Task<(decimal totalSaving, decimal totalFabricationTime)> CalculateAll(
            List<ItemRequestModel> itemRequests,
            YearsSettingModel year)
        {
            if (itemRequests == null || itemRequests.Count == 0)
                return (0m, 0m);

            decimal grandTotalSaving = 0m;
            decimal grandFabricationTime = 0m;

            var machines = await _context.Machines.ToListAsync();

            foreach (var itemReq in itemRequests)
            {
                if (itemReq == null) continue;

                var machine = machines
                    .FirstOrDefault(x => x.MachineCategoryId == itemReq.MachineCategoryId);

                if (machine == null) continue; 

                var (saving, fabTime) = Calculate(itemReq, machine, year, 1);

                grandTotalSaving += saving;
                grandFabricationTime += fabTime;
            }

            return (grandTotalSaving, grandFabricationTime);
        }


    }
}