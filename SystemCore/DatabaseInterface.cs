using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SystemCore
{
    public class DatabaseInterface
    {
        private RealtyManager RealtyManager { get; }
        public delegate void ResultDelegate(string data, string status);
        public event ResultDelegate ActionResult;

        public DatabaseInterface()
        {
            RealtyManager = new RealtyManager(this);
        }

        public void OnActionResult(string data, string status)
        {
            ActionResult?.Invoke(data, status);
        }

        /// <summary>
        /// 1.
        /// </summary>
        /// <param name="registerNumber"></param>
        /// <param name="name"></param>
        public void SearchRealtyByRegisterNumberAndName(int registerNumber, string name)
        {
            if (RealtyManager.SearchRealtyByRegisterNumberAndName(registerNumber, name, out var message))
            {
                OnActionResult(message,"Ok");
                return;
            }
            OnActionResult(message,"Failed");
        }

        /// <summary>
        /// 2.
        /// </summary>
        /// <param name="id"></param>
        public void SearchRealtyById(int id)
        {
            if (RealtyManager.SearchRealtyById(id, out var message))
            {
                OnActionResult(message, "Ok");
                return;
            }
            OnActionResult(message, "Failed");
        }

        /// <summary>
        /// 3.
        /// </summary>
        /// <param name="registerNumber"></param>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="description"></param>
        public void AddRealty(int registerNumber, int id, string name, string description)
        {
            if (RealtyManager.AddRealty(registerNumber,id,name,description, out var message))
            {
                OnActionResult(message, "Ok");
                return;
            }
            OnActionResult(message, "Failed");
        }

        /// <summary>
        /// 4.
        /// </summary>
        /// <param name="registerNumber"></param>
        /// <param name="name"></param>
        public void RemoveRealty(int registerNumber, string name)
        {
            if (RealtyManager.RemoveRealty(registerNumber, name, out var message))
            {
                OnActionResult(message, "Ok");
                return;
            }
            OnActionResult(message, "Failed");
        }

        /// <summary>
        /// 5,
        /// </summary>
        /// <param name="id"></param>
        public void UpdateRealty(int id, string  newName, string newDescription, int newRegisterNumber)
        {
            if (RealtyManager.UpdateRealty(id,newName, newDescription, newRegisterNumber, out var message))
            {
                OnActionResult(message, "Ok");
                return;
            }
            OnActionResult(message, "Failed");
        }

        public void SaveData()
        {
            RealtyManager.SaveStructures();
        }

        public void LoadData()
        {
            RealtyManager.LoadStructures();
        }

        public void ResetStructures(int blockFactor, int overflowBlockFactor, int blockInRaf)
        {
            RealtyManager.ResetStructures(blockFactor,overflowBlockFactor, blockInRaf);
        }

        public void Closing()
        {
            //SaveData();
        }

        public void GenerateData(int cadastralAreasCount, int realtiesCount)
        {
            RealtyManager.GenerateData(cadastralAreasCount, realtiesCount);
        }

        public void ShowBlocksById()
        {
            RealtyManager.ShowBlocksById();
        }

        public void ShowOverflowBlocksById()
        {
            RealtyManager.ShowOverflowBlocksById();
        }

        public void ShowBlocksByName()
        {
            RealtyManager.ShowBlocksByName();
        }

        public void ShowOverflowBlocksByName()
        {
            RealtyManager.ShowOverflowBlocksByName();
        }

        public void ShowBlocksRaf()
        {
            RealtyManager.ShowBlocksRaf();
        }
    }
}
