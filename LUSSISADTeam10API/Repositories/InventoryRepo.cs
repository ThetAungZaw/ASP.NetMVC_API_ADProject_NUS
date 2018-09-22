﻿using LUSSISADTeam10API.Constants;
using LUSSISADTeam10API.Models.APIModels;
using LUSSISADTeam10API.Models.DBModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

// Author : Zin Min Htet
namespace LUSSISADTeam10API.Repositories
{
    public class InventoryRepo
    {
        private static InventoryModel CovertDBInventorytoAPIInventory(inventory inv)
        {
            InventoryModel invm = new InventoryModel(inv.invid, inv.itemid, inv.item.description, inv.stock, inv.reorderlevel, inv.reorderqty, inv.item.category.name, inv.item.uom);
            return invm;
        }
        private static List<PurchaseOrderModel> staticpoms;
        private static int staticcount = 0;
        private static InventoryDetailModel CovertDBInventorytoAPIInventoryDet(inventory inv)
        {
            string error = "";
            LUSSISEntities entities = new LUSSISEntities();
            // to show the recommended order qty 
            int? recommededorderqty = 0;

            // if the stock is less than or equal reorder level
            if (inv.stock <= inv.reorderlevel)
            {
                // the recommended order qty will be the minimum reorder level and reorder qty and the total qty stock of outstanding req
                recommededorderqty = (inv.reorderlevel - inv.stock) + inv.reorderqty;

                List<OutstandingItemModel> outs = OutstandingReqDetailRepo.GetAllPendingOutstandingItems(out error);

                List<PurchaseOrderModel> poms = new List<PurchaseOrderModel>();

                if (error == "" && outs != null)
                {
                    try
                    {
                        if (staticpoms == null)
                        {
                            staticpoms = new List<PurchaseOrderModel>();
                        }

                        bool PendingPOExists = false;

                        if (staticcount < 4)
                        {
                            poms = staticpoms;

                            foreach (PurchaseOrderModel pom in poms)
                            {
                                int count = 0;
                                count = pom.podms.Where(x => x.Itemid == inv.itemid).Count();
                                if (count > 0)
                                {
                                    PendingPOExists = true;
                                    staticcount++;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            PendingPOExists = true;
                        }
                        if (PendingPOExists)
                        {
                            recommededorderqty = 0;
                        }
                        else
                        {
                            int itemlist = outs.Where(p => p.ItemId == inv.itemid).Count<OutstandingItemModel>();
                            if (itemlist > 0)
                            {
                                OutstandingItemModel outItem = outs.Where(p => p.ItemId == inv.itemid).FirstOrDefault<OutstandingItemModel>();
                                recommededorderqty += outItem.Total;

                            }
                        }
                    }
                    catch (Exception e)
                    {
                        error = e.Message;
                    }
                }
            }
            InventoryDetailModel invdm = new InventoryDetailModel(inv.invid, inv.itemid, inv.item.description, inv.stock, inv.reorderlevel, inv.reorderqty, inv.item.catid, inv.item.category.name, inv.item.description, inv.item.uom, recommededorderqty, inv.item.category.shelflocation, inv.item.category.shelflevel);
            return invdm;
        }
        public static List<InventoryModel> GetAllInventories(out string error)
        {
            LUSSISEntities entities = new LUSSISEntities();

            // Initializing the error variable to return only blank if there is no error
            error = "";
            List<InventoryModel> invms = new List<InventoryModel>();
            try
            {
                // get inventory list from database
                List<inventory> invs = entities.inventories.ToList<inventory>();

                // convert the DB Model list to API Model list
                foreach (inventory inv in invs)
                {
                    invms.Add(CovertDBInventorytoAPIInventory(inv));
                }
            }

            // if inventory not found, will throw NOTFOUND exception
            catch (NullReferenceException)
            {
                // if there is NULL Exception error, error will be 404
                error = ConError.Status.NOTFOUND;
            }

            catch (Exception e)
            {
                // for other exceptions
                error = e.Message;
            }

            //returning the list
            return invms;
        }
        public static InventoryModel GetInventoryByInventoryid(int inventoryid, out string error)
        {
            LUSSISEntities entities = new LUSSISEntities();

            error = "";

            inventory inventory = new inventory();
            InventoryModel invm = new InventoryModel();
            try
            {
                inventory = entities.inventories.Where(p => p.invid == inventoryid).FirstOrDefault<inventory>();
                invm = CovertDBInventorytoAPIInventory(inventory);
            }
            catch (NullReferenceException)
            {
                error = ConError.Status.NOTFOUND;
            }
            catch (Exception e)
            {
                error = e.Message;
            }
            return invm;
        }
        public static InventoryModel GetInventoryByItemid(int itemid, out string error)
        {
            LUSSISEntities entities = new LUSSISEntities();

            error = "";

            inventory inventory = new inventory();
            InventoryModel invm = new InventoryModel();
            try
            {
                inventory = entities.inventories.Where(p => p.itemid == itemid).FirstOrDefault<inventory>();
                invm = CovertDBInventorytoAPIInventory(inventory);
            }
            catch (NullReferenceException)
            {
                error = ConError.Status.NOTFOUND;
            }
            catch (Exception e)
            {
                error = e.Message;
            }
            return invm;
        }
        public static List<InventoryDetailModel> GetAllInventoryDetails(out string error)
        {
            LUSSISEntities entities = new LUSSISEntities();

            // Initializing the error variable to return only blank if there is no error
            error = "";
            List<InventoryDetailModel> invdms = new List<InventoryDetailModel>();
            try
            {
                // get inventory list from database
                List<inventory> invs = entities.inventories.Where(x => x.item.itemid == (x.item.supplieritems.Where(p => p.supplier.active == ConSupplier.Active.ACTIVE).FirstOrDefault().itemid)).ToList<inventory>();
                staticcount = 1;
                staticpoms = PurchaseOrderRepo.GetPurchaseOrderByStatus(ConPurchaseOrder.Status.PENDING, out error);
                // convert the DB Model list to API Model list for inv detail
                foreach (inventory inv in invs)
                {
                    invdms.Add(CovertDBInventorytoAPIInventoryDet(inv));
                }

            }

            // if inventory not found, will throw NOTFOUND exception
            catch (NullReferenceException)
            {
                // if there is NULL Exception error, error will be 404
                error = ConError.Status.NOTFOUND;
            }

            catch (Exception e)
            {
                // for other exceptions
                error = e.Message;
            }

            //returning the list
            return invdms;
        }
        public static InventoryDetailModel GetInventoryDetailByInventoryid(int inventoryid, out string error)
        {
            LUSSISEntities entities = new LUSSISEntities();
            error = "";
            inventory inventory = new inventory();
            InventoryDetailModel invdm = new InventoryDetailModel();
            try
            {
                staticpoms = PurchaseOrderRepo.GetPurchaseOrderByStatus(ConPurchaseOrder.Status.PENDING, out error);
                staticcount = 1;

                inventory = entities.inventories.Where(p => p.invid == inventoryid).FirstOrDefault<inventory>();
                invdm = CovertDBInventorytoAPIInventoryDet(inventory);
            }
            catch (NullReferenceException)
            {
                error = ConError.Status.NOTFOUND;
            }
            catch (Exception e)
            {
                error = e.Message;
            }
            return invdm;
        }
        public static InventoryDetailModel GetInventoryDetailByItemid(int itemid, out string error)
        {
            LUSSISEntities entities = new LUSSISEntities();
            error = "";
            inventory inventory = new inventory();
            InventoryDetailModel invdm = new InventoryDetailModel();
            try
            {
                staticpoms = PurchaseOrderRepo.GetPurchaseOrderByStatus(ConPurchaseOrder.Status.PENDING, out error);
                staticcount = 1;

                inventory = entities.inventories.Where(p => p.itemid == itemid).FirstOrDefault<inventory>();
                invdm = CovertDBInventorytoAPIInventoryDet(inventory);
            }
            catch (NullReferenceException)
            {
                error = ConError.Status.NOTFOUND;
            }
            catch (Exception e)
            {
                error = e.Message;
            }
            return invdm;
        }
        public static InventoryModel UpdateInventory(InventoryModel invm, out string error)
        {
            error = "";
            // declare and initialize new LUSSISEntities to perform update
            LUSSISEntities entities = new LUSSISEntities();
            inventory inv = new inventory();
            try
            {
                // finding the inventory object using Inventory API model
                inv = entities.inventories.Where(p => p.invid == invm.Invid).First<inventory>();

                // transfering data from API model to DB Model
                inv.itemid = invm.Itemid;
                inv.stock = invm.Stock;
                inv.reorderlevel = invm.ReorderLevel;
                inv.reorderqty = invm.ReorderQty;

                // saving the update
                entities.SaveChanges();

                // return the updated model 
                invm = CovertDBInventorytoAPIInventory(inv);
            }
            catch (NullReferenceException)
            {
                error = ConError.Status.NOTFOUND;
            }
            catch (Exception e)
            {
                error = e.Message;
            }
            return invm;
        }
        public static InventoryModel CreateInventory(InventoryModel invm, out string error)
        {
            error = "";
            LUSSISEntities entities = new LUSSISEntities();
            inventory inv = new inventory();
            try
            {
                inv.itemid = invm.Itemid;
                inv.stock = invm.Stock;
                inv.reorderlevel = invm.ReorderLevel;
                inv.reorderqty = invm.ReorderQty;
                inv = entities.inventories.Add(inv);
                entities.SaveChanges();
                // retrieving the inserted inventory model by using the GetInventory method
                invm = GetInventoryByInventoryid(inv.invid, out error);
            }
            catch (NullReferenceException)
            {
                error = ConError.Status.NOTFOUND;
            }
            catch (Exception e)
            {
                error = e.Message;
            }
            return invm;
        }
        public static Boolean RemoveInventory(InventoryModel invm, out string error)
        {
            error = "";
            LUSSISEntities entities = new LUSSISEntities();
            inventory inv = new inventory();
            try
            {
                if (entities.inventories.Where(p => p.itemid == invm.Itemid).Count() > 0)
                {
                    inv = entities.inventories.Where(p => p.invid == invm.Itemid).First<inventory>();
                    entities.inventories.Remove(inv);
                    entities.SaveChanges();
                }
            }
            catch (NullReferenceException)
            {
                error = ConError.Status.NOTFOUND;
                return false;
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }
            return true;
        }
        public static List<InventoryDetailWithStatus> GetInventoryDetailWithStatus(out string error)
        {
            LUSSISEntities entities = new LUSSISEntities();
            error = "";
            List<InventoryDetailWithStatus> invdms = new List<InventoryDetailWithStatus>();
            try
            {
                List<adjustmentdetail> adjds = entities.adjustmentdetails.Where(x => x.adjustment.status == ConAdjustment.Active.PENDING).ToList();

                List<inventory> invs = entities.inventories.ToList<inventory>();
                foreach (inventory inv in invs)
                {
                    bool IsPending = false;
                    int count = adjds.Where(x => x.itemid == inv.itemid).Count();
                    InventoryDetailWithStatus invdm = new InventoryDetailWithStatus();
                    int CurrentStock = inv.stock;
                    string Reason = "";
                    if (count > 0)
                    {
                        adjustmentdetail adj = adjds.Where(x => x.itemid == inv.itemid).FirstOrDefault();
                        IsPending = true;
                        CurrentStock = adj.adjustedqty + inv.stock;
                        Reason = adj.reason;
                    }
                    invdm = new InventoryDetailWithStatus(inv.invid, inv.itemid, inv.item.description, inv.stock, inv.reorderlevel, inv.reorderqty, inv.item.catid, inv.item.category.name,
                        inv.item.description, inv.item.uom, IsPending, inv.item.category.shelflocation,
                        inv.item.category.shelflevel, CurrentStock, Reason);
                    invdms.Add(invdm);
                }

            }

            catch (NullReferenceException)
            {
                error = ConError.Status.NOTFOUND;
            }

            catch (Exception e)
            {
                // for other exceptions
                error = e.Message;
            }

            //returning the list
            return invdms;
        }
    }
}