using EWOS_MVC.Models;

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
                        (((item.Weight ?? 0m) / 1000)
                         * (item.RawMaterials?.Price ?? 0m));

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
    }
}