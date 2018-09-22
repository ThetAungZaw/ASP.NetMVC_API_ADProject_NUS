﻿using LUSSISADTeam10API.Constants;
using LUSSISADTeam10API.Models.APIModels;
using LUSSISADTeam10API.Models.DBModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

// Author : Htet Wai Yan | Aung Myo
namespace LUSSISADTeam10API.Repositories
{
    public static class SupplierItemRepo
    {
        // Convert From Auto Generated DB Model to APIModel
        private static SupplierItemModel ConvertDBSupItemToAPISupItem(supplieritem supitem)
        {
            return new SupplierItemModel(
                    supitem.supid,
                    supitem.supplier.supname,
                    supitem.itemid,
                    supitem.item.description,
                    supitem.price,
                    supitem.item.uom,
                    supitem.item.category.name
                );
        }

        // Get all items of all suppliers
        public static List<SupplierItemModel> GetAllSupplierItem(out string error)
        {
            LUSSISEntities entities = new LUSSISEntities();
            error = "";
            List<SupplierItemModel> sims = new List<SupplierItemModel>();
            try
            {
                List<supplieritem> supplieritems = entities.supplieritems.Where(x => x.supplier.active == ConSupplier.Active.ACTIVE).ToList();
                sims = new List<SupplierItemModel>();
                foreach (supplieritem si in supplieritems)
                {
                    sims.Add(ConvertDBSupItemToAPISupItem(si));
                }
            }
            catch (NullReferenceException)
            {
                error = ConError.Status.NOTFOUND;
            }
            catch (Exception e)
            {
                error = e.Message;
            }
            return sims;
        }

        // Get all items from by specific supplier
        public static List<SupplierItemModel> GetItemsBySupplier(int supid, out string error)
        {
            LUSSISEntities entities = new LUSSISEntities();
            error = "";

            supplier sup = new supplier();
            List<SupplierItemModel> sims = new List<SupplierItemModel>();
            try
            {
                sup = entities.suppliers
                    .Where(x => x.supid == supid)
                    .First();
                foreach (supplieritem supitem in sup.supplieritems)
                {
                    sims.Add(ConvertDBSupItemToAPISupItem(supitem));
                }
            }
            catch (NullReferenceException)
            {
                error = ConError.Status.NOTFOUND;
            }
            catch (Exception e)
            {
                error = e.Message;
            }
            return sims;
        }

        // Get item price by itemid
        public static SupplierItemModel GetSupplierItemByItemId(int itemid, out string error)
        {
            LUSSISEntities entities = new LUSSISEntities();
            error = "";

            supplieritem supitem = new supplieritem();
            SupplierItemModel sim = new SupplierItemModel();
            try
            {
                double min = 0.0;
                min = entities.supplieritems
                    .Where(x => x.itemid == itemid)
                    .Min(x => x.price);
                supitem = entities.supplieritems
                    .Where(x => x.price == min && x.itemid == itemid)
                    .FirstOrDefault();
                sim = ConvertDBSupItemToAPISupItem(supitem);
            }
            catch (NullReferenceException)
            {
                error = ConError.Status.NOTFOUND;
            }
            catch (InvalidOperationException)
            {
                error = ConError.Status.BADREQUEST;
            }
            catch (Exception e)
            {
                error = e.Message;
            }
            return sim;
        }

        // Get item price by itemid
        public static List<SupplierItemModel> GetSupplierItemListByItemId(int itemid, out string error)
        {
            LUSSISEntities entities = new LUSSISEntities();
            error = "";

            List<supplieritem> supitems = new List<supplieritem>();
            List<SupplierItemModel> sims = new List<SupplierItemModel>();

            try
            {
                supitems = entities.supplieritems
                    .Where(x => x.itemid == itemid).ToList();
                foreach (supplieritem si in supitems)
                {
                    sims.Add(ConvertDBSupItemToAPISupItem(si));
                }
            }
            catch (NullReferenceException)
            {
                error = ConError.Status.NOTFOUND;
            }
            catch (InvalidOperationException)
            {
                error = ConError.Status.BADREQUEST;
            }
            catch (Exception e)
            {
                error = e.Message;
            }
            return sims;
        }

        public static SupplierItemModel GetSupplierItemListByItemIdandSupid(int itemid, int supid, out string error)
        {
            LUSSISEntities entities = new LUSSISEntities();
            error = "";

            supplieritem supitems = new supplieritem();

            SupplierItemModel supim = new SupplierItemModel();
            try
            {
                supitems = entities.supplieritems
                    .Where(x => x.itemid == itemid && x.supid == supid).First();

                supim = ConvertDBSupItemToAPISupItem(supitems);



            }
            catch (NullReferenceException)
            {
                error = ConError.Status.NOTFOUND;
            }
            catch (InvalidOperationException)
            {
                error = ConError.Status.BADREQUEST;
            }
            catch (Exception e)
            {
                error = e.Message;
            }
            return supim;
        }




















        // Add item by specific supplier
        public static SupplierItemModel AddItemOfSupplier
            (SupplierItemModel sim, out string error)
        {
            LUSSISEntities entities = new LUSSISEntities();
            error = "";

            supplieritem supitem = new supplieritem();
            try
            {
                supitem.supid = sim.SupId;
                supitem.itemid = sim.ItemId;
                supitem.price = sim.Price;
                entities.supplieritems.Add(supitem);
                entities.SaveChanges();
               
            }
            catch (Exception ex) {

                error = ex.Message;
            }
            return sim;
        }

        // Update item by specific supplier
        public static SupplierItemModel UpdateSupplierItem
            (SupplierItemModel sim, out string error)
        {
            LUSSISEntities entities = new LUSSISEntities();
            error = "";
            try
            {
                supplieritem supitem = entities.supplieritems
                    .Where(x => x.supid == sim.SupId &&
                    x.itemid == sim.ItemId).First();
                supitem.price = sim.Price;
                entities.SaveChanges();
            }

            catch (Exception ex)
            {

                error = ex.Message;
            }
            return sim;
        }

        //import with csv format file

        public static List<SupplierItemModel> csvsupplier(List<SupplierItemModel> csp, out string error)
        {
            bool test = false;
            int supid = 0;
            LUSSISEntities entities = new LUSSISEntities();
            List<SupplierItemModel> smretrun = new List<SupplierItemModel>();
           error = "";
            try
            {
                foreach (SupplierItemModel sm in csp)
                {
                    supid = sm.SupId;
                    List<SupplierItemModel> csp1 = GetItemsBySupplier(supid, out string error1);
                    foreach (SupplierItemModel sm1 in csp1)
                    {

                        if (sm.Description == sm1.Description)
                        {
                            test = true;
                            UpdateSupplierItem(sm, out string error2);
                        }
                    }
                    if (test == false)
                    {
                        AddItemOfSupplier(sm, out string error3);
                    }
                    else
                    {
                        test = false;
                    }
                }
                smretrun = GetItemsBySupplier(supid, out string error4);


            }

            catch (Exception ex)
            {

                error = ex.Message;
            }
            return smretrun;

        }



        public static List<SupplierItemModel> importsupplieritem(List<SupplierItemModel> csp, out string error)
        {
            bool test = false;
            int supid = 0;
            LUSSISEntities entities = new LUSSISEntities();
            error = "";
            List<SupplierItemModel> smretrun = new List<SupplierItemModel>();
            try
            {
                foreach (SupplierItemModel sm in csp)
                {
                    supid = sm.SupId;
                    List<SupplierItemModel> csp1 = GetItemsBySupplier(supid, out string error1);
                    foreach (SupplierItemModel sm1 in csp1)
                    {

                        if (sm.Description == sm1.Description)
                        {
                            test = true;
                            UpdateSupplierItem(sm, out string error2);
                        }
                    }
                    if (test == false)
                    {

                        ItemModel im = ItemRepo.GetItemByItemid(sm.ItemId, out string error2);
                        if (im != null)
                        {
                            AddItemOfSupplier(sm, out string error3);
                            im = null;


                        }



                        test = false;
                    }
                    else
                    {
                        test = false;
                    }
                }
                smretrun = GetItemsBySupplier(supid, out string error4);
            }
            catch (Exception ex)
            {

                error = ex.Message;
            }
            return smretrun;

        }

        public static List<SupplierItemModel> ImportfromItemlistExcel(List<ImportSupplierItem> csp, out string error)
        {

            LUSSISEntities entities = new LUSSISEntities();
            List<SupplierItemModel> createdsupitemlist = new List<SupplierItemModel>();
            error = "";

            try
            {
                foreach (ImportSupplierItem sm in csp)
                {

                    SupplierModel spm = SupplierRepo.GetSupplierBySupname(sm.SupName, out string error1);

                    if (spm.SupName != "")
                    {

                        ItemModel im1 = ItemRepo.GetItemByItemDescription(sm.Description, out string error3);

                        if (im1.Description != "")
                        {

                            SupplierItemModel sim = GetSupplierItemListByItemIdandSupid(im1.Itemid, spm.SupId, out string error5);
                            if (sim.SupId != 0)
                            {

                                supplieritem supitem = entities.supplieritems
                            .Where(x => x.supid == spm.SupId &&
                            x.itemid == sim.ItemId).First();
                                supitem.price = sm.Price;
                                entities.SaveChanges();
                                createdsupitemlist.Add(GetSupplierItemListByItemIdandSupid(im1.Itemid, sim.SupId, out string error7));
                            }
                            else
                            {

                                supplieritem supitem = new supplieritem();
                                supitem.supid = spm.SupId;
                                supitem.itemid = im1.Itemid;
                                supitem.price = sm.Price;
                                entities.supplieritems.Add(supitem);
                                entities.SaveChanges();
                                createdsupitemlist.Add(GetSupplierItemListByItemIdandSupid(im1.Itemid, spm.SupId, out string error8));
                            }

                        }

                        else
                        {
                            item item = new item();


                            item.catid = 21;
                            item.description = sm.Description;
                            item.uom = sm.Uom;
                            item = entities.items.Add(item);
                            entities.SaveChanges();

                            ItemModel im = ItemRepo.GetItemByItemid(item.itemid, out error);


                            inventory ivm = new inventory();
                            ivm.itemid = im.Itemid;
                            ivm.stock = 0;
                            ivm.reorderqty = 0;
                            ivm.reorderlevel = 0;
                            ivm = entities.inventories.Add(ivm);
                            entities.SaveChanges();


                            supplieritem supitem = new supplieritem();
                            supitem.supid = spm.SupId;
                            supitem.itemid = im.Itemid;
                            supitem.price = sm.Price;
                            entities.supplieritems.Add(supitem);
                            entities.SaveChanges();
                            createdsupitemlist.Add(GetSupplierItemListByItemIdandSupid(im.Itemid, spm.SupId, out string error5));

                        }
                    }
                }
            }
            catch (Exception ex) {

                error = ex.Message;

            }
            return createdsupitemlist;

        }



    }
}