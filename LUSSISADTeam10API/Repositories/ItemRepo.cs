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
    public class ItemRepo
    {
        // Convert From Auto Generated DB Model to APIModel
        private static ItemModel CovertDBItemtoAPIItem(item item)
        {
            ItemModel im = new ItemModel(item.itemid, item.catid, item.category.name, item.description, item.uom);
            return im;
        }
        // Get the list of all items and will return with error if there is one.
        public static List<ItemModel> GetAllItems(out string error)
        {
            LUSSISEntities entities = new LUSSISEntities();

            // Initializing the error variable to return only blank if there is no error
            error = "";
            List<ItemModel> ims = new List<ItemModel>();
            try
            {
                // get item list from database
                List<item> items = entities.items.ToList<item>();

                // convert the DB Model list to API Model list
                foreach (item item in items)
                {
                    ims.Add(CovertDBItemtoAPIItem(item));
                }
            }

            // if item not found, will throw NOTFOUND exception
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
            return ims;
        }


        public static List<ItemModel> GetAllSupplierItems(out string error)
        {
            LUSSISEntities entities = new LUSSISEntities();
            error = "";
            List<ItemModel> ims = new List<ItemModel>();
            try
            {
                List<item> items = entities.items.Where(x => x.itemid == (x.supplieritems.Where(p => p.supplier.active == ConSupplier.Active.ACTIVE).FirstOrDefault().itemid)).ToList<item>();

                foreach (item item in items)
                {
                    ims.Add(CovertDBItemtoAPIItem(item));
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
            return ims;
        }
        public static ItemModel GetItemByItemid(int itemid, out string error)
        {
            LUSSISEntities entities = new LUSSISEntities();

            error = "";

            item item = new item();
            ItemModel im = new ItemModel();
            try
            {
                item = entities.items.Where(p => p.itemid == itemid).FirstOrDefault<item>();
                im = CovertDBItemtoAPIItem(item);
            }
            catch (NullReferenceException)
            {
                error = ConError.Status.NOTFOUND;
            }
            catch (Exception e)
            {
                error = e.Message;
            }
            return im;
        }
        public static List<ItemModel> GetItemByCatid(int catid, out string error)
        {
            LUSSISEntities entities = new LUSSISEntities();
            error = "";

            List<ItemModel> item = new List<ItemModel>();
            List<category> cat = new List<category>();
            List<item> it = new List<item>();

            try
            {
                it = entities.items.Where(p => p.catid == catid).ToList<item>();
                foreach (item i in it)
                {
                    item.Add(CovertDBItemtoAPIItem(i));
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
            return item;
        }

        public static ItemModel GetItemsByDisid(int disid, out string error)
        {
            LUSSISEntities entities = new LUSSISEntities();

            error = "";

            item item = new item();
            disbursementdetail disdet = new disbursementdetail();
            ItemModel im = new ItemModel();
            try
            {
                disdet = entities.disbursementdetails.Where(p => p.disid == disid).FirstOrDefault<disbursementdetail>();
                item = disdet.item;
                im = CovertDBItemtoAPIItem(item);
            }
            catch (NullReferenceException)
            {
                error = ConError.Status.NOTFOUND;
            }
            catch (Exception e)
            {
                error = e.Message;
            }
            return im;
        }
        public static ItemModel GetItemByPoid(int poid, out string error)
        {
            LUSSISEntities entities = new LUSSISEntities();

            error = "";

            item item = new item();
            purchaseorderdetail podet = new purchaseorderdetail();
            ItemModel im = new ItemModel();
            try
            {
                podet = entities.purchaseorderdetails.Where(p => p.poid == poid).FirstOrDefault<purchaseorderdetail>();
                item = podet.item;
                im = CovertDBItemtoAPIItem(item);
            }
            catch (NullReferenceException)
            {
                error = ConError.Status.NOTFOUND;
            }
            catch (Exception e)
            {
                error = e.Message;
            }
            return im;
        }
        public static ItemModel GetItemByReqid(int reqid, out string error)
        {
            LUSSISEntities entities = new LUSSISEntities();

            error = "";
            item item = new item();
            requisitiondetail reqdet = new requisitiondetail();
            ItemModel im = new ItemModel();
            try
            {
                reqdet = entities.requisitiondetails.Where(p => p.reqid == reqid).FirstOrDefault<requisitiondetail>();
                item = reqdet.item;
                im = CovertDBItemtoAPIItem(item);
            }
            catch (NullReferenceException)
            {
                error = ConError.Status.NOTFOUND;
            }
            catch (Exception e)
            {
                error = e.Message;
            }
            return im;
        }
        public static ItemModel GetItemBySupid(int supid, out string error)
        {
            LUSSISEntities entities = new LUSSISEntities();

            error = "";
            item item = new item();
            supplieritem supdet = new supplieritem();
            ItemModel im = new ItemModel();
            try
            {
                supdet = entities.supplieritems.Where(p => p.supid == supid).FirstOrDefault<supplieritem>();
                item = supdet.item;
                im = CovertDBItemtoAPIItem(item);
            }
            catch (NullReferenceException)
            {
                error = ConError.Status.NOTFOUND;
            }
            catch (Exception e)
            {
                error = e.Message;
            }
            return im;
        }
        public static ItemModel UpdateItem(ItemModel im, out string error)
        {
            error = "";
            // declare and initialize new LUSSISEntities to perform update
            LUSSISEntities entities = new LUSSISEntities();
            item item = new item();
            try
            {
                // finding the item object using Item API model
                item = entities.items.Where(p => p.itemid == im.Itemid).First<item>();

                // transfering data from API model to DB Model
                item.catid = im.Catid;
                item.description = im.Description;
                item.uom = im.Uom;

                // saving the update
                entities.SaveChanges();

                // return the updated model 
                im = CovertDBItemtoAPIItem(item);
            }
            catch (NullReferenceException)
            {
                error = ConError.Status.NOTFOUND;
            }
            catch (Exception e)
            {
                error = e.Message;
            }
            return im;
        }
        public static ItemModel CreateItem(ItemModel im, out string error)
        {
            error = "";
            LUSSISEntities entities = new LUSSISEntities();
            item item = new item();
            try
            {
                item.catid = im.Catid;
                item.description = im.Description;
                item.uom = im.Uom;
                item = entities.items.Add(item);
                entities.SaveChanges();
                // retrieving the inserted item model by using the GetItem method
                im = GetItemByItemid(item.itemid, out error);
            }
            catch (NullReferenceException)
            {
                error = ConError.Status.NOTFOUND;
            }
            catch (Exception e)
            {
                error = e.Message;
            }
            return im;
        }
        public static ItemModel GetItemByItemDescription(string name, out string error)
        {
            LUSSISEntities entities = new LUSSISEntities();

            error = "";

            item item = new item();
            ItemModel im = new ItemModel();
            try
            {
                item = entities.items.Where(p => p.description == name).FirstOrDefault<item>();
                im = CovertDBItemtoAPIItem(item);
            }
            catch (NullReferenceException)
            {
                error = ConError.Status.NOTFOUND;
            }
            catch (Exception e)
            {
                error = e.Message;
            }
            return im;
        }
    }
}