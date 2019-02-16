using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DynamicHash;

namespace SystemCore
{
    internal enum RealtyUpdateChange
    {
        None,
        JustDescription,
        Everything
    }
    internal class RealtyManager
    {
        //dyn hash 1
        //dyn hash 2
        //raf
        public DatabaseInterface DbInterface { get; set; }

        public DynHash<RealtyByCadAndRegNumber> DhRealtyByNameAndRegNumber { get; set; }
        public DynHash<RealtyById> DhRealtyById { get; set; }
        public RandomAccessFile<Realty> RafRealties { get; set; }

        private const int DefaultBlockFactor = 20;
        private const int DefaultOverflowBlockFactor = 50;

        public RealtyManager(DatabaseInterface dbInterface)
        {
            DbInterface = dbInterface;

           var byNameFile = new FilePaths
            {
                FileBlockData =
                    "../../../SystemCore/data/ByNameAndReg/blocks.txt",
                FileOverflowFile =
                    "../../../SystemCore/data/ByNameAndReg/overflow.txt",
                FileTreeData =
                    "../../../SystemCore/data/ByNameAndReg/configuration.txt"
            };
            DhRealtyByNameAndRegNumber = new DynHash<RealtyByCadAndRegNumber>(byNameFile, DefaultBlockFactor, DefaultOverflowBlockFactor);
            var byId = new FilePaths
            {
                FileBlockData = "../../../SystemCore/data/ById/blocks.txt",
                FileOverflowFile = "../../../SystemCore/data/ById/overflow.txt",
                FileTreeData = "../../../SystemCore/data/ById/configuration.txt"
            };
            DhRealtyById = new DynHash<RealtyById>(byId, DefaultBlockFactor, DefaultOverflowBlockFactor);

            var rafBlockFile = "../../../SystemCore/data/Raf/blocks.txt";
            var rafConfFile = "../../../SystemCore/data/Raf/configuration.txt";
            RafRealties = new RandomAccessFile<Realty>(rafBlockFile, rafConfFile, DefaultBlockFactor);
        }



        /// <summary>
        /// 1.
        /// </summary>
        /// <param name="registerNumber"></param>
        /// <param name="name"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public bool SearchRealtyByRegisterNumberAndName(int registerNumber, string name, out string message)
        {
            Realty.EqualsOptions = EqualsOptions.ByNameAndRegNumber;
            var helpRealty = new Realty(registerNumber, name);
            var helpByName = new RealtyByCadAndRegNumber(helpRealty);

            if (DhRealtyByNameAndRegNumber.TryFind(helpByName, out var record))
            {
                if (RafRealties.TryFind(helpRealty, record.AddressIndex, out var realty))
                {
                    message = realty.ToString();
                    return true;
                }
            }
            message = $"Realty with register number: {registerNumber} and name: {name} not found";
            return false;
        }

        /// <summary>
        /// 2.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public bool SearchRealtyById(int id, out string message)
        {
            Realty.EqualsOptions = EqualsOptions.ById;
            var helpRealty = new Realty(id);
            var helpById = new RealtyById(helpRealty);

            if (DhRealtyById.TryFind(helpById, out var record))
            {
                if (RafRealties.TryFind(helpRealty, record.AddressIndex, out var realty))
                {
                    message = realty.ToString();
                    return true;
                }
            }
            message = $"Realty with id: {id} not found";
            return false;
        }

        /// <summary>
        /// 3.
        /// </summary>
        /// <param name="registerNumber"></param>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="description"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public bool AddRealty(int registerNumber, int id, string name, string description, out string message)
        {
            try
            {
                Realty.EqualsOptions = EqualsOptions.ById;
                var realty = new Realty(registerNumber, name, description, id);

                if (RafRealties.Add(realty, out var index))
                {
                    var byName = new RealtyByCadAndRegNumber(realty);
                    byName.AddressIndex = index;
                    if (!DhRealtyByNameAndRegNumber.Add(byName))
                    {
                        message =
                            $"System already contains realty with name: {name} and register number {registerNumber} and id {id}";
                        RafRealties.TryRemove(realty, index, out _);
                        return false;
                    }
                    var byId = new RealtyById(realty);
                    byId.AddressIndex = index;
                    if (!DhRealtyById.Add(byId))
                    {
                        message =
                            $"System already contains realty with name: {name} and register number {registerNumber} and id {id}";
                        DhRealtyByNameAndRegNumber.TryRemove(byName, out _);
                        RafRealties.TryRemove(realty, index, out _);
                        return false;
                    }

                    message = "New realty added";
                    return true;
                }
                message =
                    $"System already could not add realty with name: {name} and register number {registerNumber} and id {id}";
                return false;
            }
            catch (ArgumentOutOfRangeException)
            {
                message = "Could not create realty, name or description too long";
                return false;
            }
        }

        /// <summary>
        /// 4.
        /// </summary>
        /// <param name="registerNumber"></param>
        /// <param name="name"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public bool RemoveRealty(int registerNumber, string name, out string message)
        {
            Realty.EqualsOptions = EqualsOptions.ByNameAndRegNumber;
            var helpRealty = new Realty(registerNumber, name);
            var helpByName = new RealtyByCadAndRegNumber(helpRealty);

            if (DhRealtyByNameAndRegNumber.TryRemove(helpByName, out var record))
            {
                if (RafRealties.TryRemove(helpRealty, record.AddressIndex, out var realty))
                {
                    message = "Realty removed";
                    var helpById = new RealtyById(realty);
                    if (!DhRealtyById.TryRemove(helpById, out _))
                        throw new Exception("did not remove and should have..");
                    return true;
                }
            }
            message = $"Realty with register number: {registerNumber} and name: {name} not found";
            return false;
        }

        /// <summary>
        /// 5.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public bool UpdateRealty(int id, string newName, string newDescription, int newRegisterNumber, out string message)
        {
            Realty.EqualsOptions = EqualsOptions.ById;
            var helpRealty = new Realty(id);
            var helpById = new RealtyById(helpRealty);

            var newRealty = new Realty(newRegisterNumber, newName, newDescription, newRegisterNumber) {Id = id};

            if (DhRealtyById.TryFind(helpById, out var realtyById))
            {
                var index = realtyById.AddressIndex;
                if (RafRealties.BeginUpdate(helpRealty, index, out var oldRealty))
                {
                    switch (DecideRealtyUpdate(oldRealty, newRealty))
                    {
                        case RealtyUpdateChange.None:
                            {
                                //nic sa nemusi diat
                                message = "No changes needed";
                                return false;
                            }
                        case RealtyUpdateChange.JustDescription:
                            {
                                //v pamati je ulozeny blok v ktorom nie je povodna nehnutelnost, dam novu na index a v subore sa to prepise
                                RafRealties.FinishUpdate(newRealty, index);
                                message = $"Just Description changed from {oldRealty.Description} to {newRealty.Description}";
                                return true;
                            }
                        case RealtyUpdateChange.Everything:
                            {
                                var helpByName = new RealtyByCadAndRegNumber(oldRealty);
                                if (DhRealtyByNameAndRegNumber.TryRemove(helpByName, out var removedRealtyByName))
                                {
                                    var newRealtyByName = new RealtyByCadAndRegNumber(newRealty);
                                    newRealtyByName.AddressIndex = index;
                                    if (DhRealtyByNameAndRegNumber.Add(newRealtyByName))
                                    {
                                        RafRealties.FinishUpdate(newRealty, index);
                                        message =
                                            $"Name and Register number updated from {oldRealty.UniqueName} and {oldRealty.RegisterNumber} to {newRealty.UniqueName} and {newRealty.RegisterNumber}";
                                        return true;
                                    }
                                    else
                                    {
                                        DhRealtyByNameAndRegNumber.Add(removedRealtyByName);
                                        message = $"Could not update realty, no changes, realty with name and reg number {newRealty.UniqueName}, {newRealty.RegisterNumber} already exists";
                                        return false;
                                    }
                                }
                                break;
                            }
                    }
                }
            }
            message = "Could not update realty";

            return false;
        }

        private RealtyUpdateChange DecideRealtyUpdate(Realty oldRealty, Realty newRealty)
        {
            if (oldRealty.CompleteEqual(newRealty))
                return RealtyUpdateChange.None;
            if (oldRealty.UniqueNameOrRegNumberNotEqual(newRealty))
                return RealtyUpdateChange.Everything;
            if (oldRealty.JustDifferentDescriptions(newRealty))
                return RealtyUpdateChange.JustDescription;
            return RealtyUpdateChange.None;
        }

        public void ResetStructures(int blockFactor, int overflowBlockFactor, int blockInRaf)
        {
            DhRealtyById.MaxRecords = blockFactor;
            DhRealtyById.MaxRecordsInOverflow = overflowBlockFactor;
            DhRealtyById.Clear();

            DhRealtyByNameAndRegNumber.MaxRecords = blockFactor;
            DhRealtyByNameAndRegNumber.MaxRecordsInOverflow = overflowBlockFactor;
            DhRealtyByNameAndRegNumber.Clear();

            RafRealties.MaxRecords = blockInRaf;
            RafRealties.Clear();

            DbInterface.OnActionResult("Files cleaned, new structures initialized", "Ok");
        }

        public void SaveStructures()
        {
            DhRealtyById.SaveTreeData();
            DhRealtyByNameAndRegNumber.SaveTreeData();
            RafRealties.SaveConfData();

            DbInterface.OnActionResult("Data saved", "Ok");
        }

        public void LoadStructures()
        {
            DhRealtyById.LoadTreeData();
            DhRealtyById.ResetFileDataSize();
            DhRealtyByNameAndRegNumber.LoadTreeData();
            DhRealtyByNameAndRegNumber.ResetFileDataSize();
            RafRealties.LoadConfData();
            RafRealties.ResetFileDataSize();
            DbInterface.OnActionResult("Data loaded", "Ok");
        }

        public void GenerateData(int cadastralAreasCount, int realtiesCount)
        {
            var realtyId = 1;

            for (int i = 0; i < cadastralAreasCount; i++)
            {
                var registerNumber = 1;
                var cadName = "Cad name " + i;
                for (int j = 0; j < realtiesCount; j++)
                {
                    if (!AddRealty(registerNumber, realtyId, cadName, "description", out var s))
                    {
                        throw new Exception($"something went wrong during generating data, reg n: {registerNumber}, id: {realtyId}, name: {cadName} \n {s}");
                    }

                    registerNumber++;
                    realtyId++;

                }
            }
            DbInterface.OnActionResult("Data generated", "Ok");
        }

        public void ShowBlocksById()
        {
            DbInterface.OnActionResult(DhRealtyById.WriteAllBlocks().ToString(), "Ok");
        }

        public void ShowOverflowBlocksById()
        {
            DbInterface.OnActionResult(DhRealtyById.WriteAllOverflowBlocks().ToString(), "Ok");
        }

        public void ShowBlocksByName()
        {
            DbInterface.OnActionResult(DhRealtyByNameAndRegNumber.WriteAllBlocks().ToString(), "Ok");
        }

        public void ShowOverflowBlocksByName()
        {
            DbInterface.OnActionResult(DhRealtyByNameAndRegNumber.WriteAllOverflowBlocks().ToString(), "Ok");
        }

        public void ShowBlocksRaf()
        {
            DbInterface.OnActionResult(RafRealties.WriteAllBlocks().ToString(), "Ok");
        }
    }
}
