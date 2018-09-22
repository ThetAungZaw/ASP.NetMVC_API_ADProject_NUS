﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LUSSISADTeam10API.Models.DBModels;
using LUSSISADTeam10API.Models.APIModels;
using LUSSISADTeam10API.Constants;

// Author : Hsu Yee Phyo

namespace LUSSISADTeam10API.Repositories
{
    public class CategoryRepo
    {
        private static CategoryModel ConvertDBCategorytoAPICategory(category cat)
        {
            LUSSISEntities entities = new LUSSISEntities();
            CategoryModel catm = new CategoryModel(cat.catid, cat.name, cat.shelflocation, cat.shelflevel);
            return catm;
        }

        //All category list
        public static List<CategoryModel> GetAllCategory(out string error)
        {
            LUSSISEntities entities = new LUSSISEntities();
            error = "";
            List<CategoryModel> catm = new List<CategoryModel>();
            try
            {
                List<category> categories = entities.categories.Where(c => c.name != "Default").ToList<category>();
                catm = new List<CategoryModel>();
                foreach (category c in categories)
                {
                    catm.Add(ConvertDBCategorytoAPICategory(c));
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
            return catm;
        }
        //Get Category by Category ID
        public static CategoryModel GetCategoryByCatId(int catid, out string error)
        {
            LUSSISEntities entities = new LUSSISEntities();
            error = "";
            CategoryModel cat = new CategoryModel();
            category category = new category();
            try
            {
                category = entities.categories.Where(c => c.catid == catid).FirstOrDefault<category>();
                cat = ConvertDBCategorytoAPICategory(category);
            }
            catch (NullReferenceException)
            {
                error = ConError.Status.NOTFOUND;
            }
            catch (Exception e)
            {
                error = e.Message;
            }
            return cat;
        }
        public static CategoryModel GetCategorybyCatName(string catname, out string error)
        {
            LUSSISEntities entities = new LUSSISEntities();
            error = "";

            category cp = new category();

            CategoryModel cpm = new CategoryModel();
            try
            {
                cp = entities.categories.Where(p => p.name == catname).FirstOrDefault<category>();
                cpm = ConvertDBCategorytoAPICategory(cp);
            }
            catch (NullReferenceException)
            {
                error = ConError.Status.NOTFOUND;
            }
            catch (Exception e)
            {
                error = e.Message;
            }
            return cpm;
        }
        //Get Category by Item ID
        public static CategoryModel GetCategoryByItemId(int itemid, out string error)
        {
            LUSSISEntities entities = new LUSSISEntities();
            error = "";
            item item = new item();
            CategoryModel cat = new CategoryModel();
            try
            {
                item = entities.items.Where(i => i.itemid == itemid).First<item>();
                category category = item.category;
                cat = ConvertDBCategorytoAPICategory(category);
            }
            catch (NullReferenceException)
            {
                error = ConError.Status.NOTFOUND;
            }
            catch (Exception e)
            {
                error = e.Message;
            }
            return cat;
        }
        //Update Category
        public static CategoryModel UpdateCategory(CategoryModel cat, out string error)
        {
            error = "";
            LUSSISEntities entities = new LUSSISEntities();
            category ca = new category();
            try
            {
                ca = entities.categories.Where(c => c.catid == cat.Catid).First<category>();
                ca.name = cat.Name;
                ca.shelflocation = cat.Shelflocation;
                ca.shelflevel = cat.Shelflevel;
                entities.SaveChanges();
                cat = ConvertDBCategorytoAPICategory(ca);
            }
            catch (NullReferenceException)
            {
                error = ConError.Status.NOTFOUND;
            }
            catch (Exception e)
            {
                error = e.Message;
            }
            return cat;
        }
        //Create Category
        public static CategoryModel CreateCategory(CategoryModel cat, out string error)
        {
            error = "";
            LUSSISEntities entities = new LUSSISEntities();
            category c = new category();
            try
            {
                c.name = cat.Name;
                c.shelflocation = cat.Shelflocation;
                c.shelflevel = cat.Shelflevel;
                c = entities.categories.Add(c);
                entities.SaveChanges();
                cat = ConvertDBCategorytoAPICategory(c);
            }
            catch (NullReferenceException)
            {
                error = ConError.Status.NOTFOUND;
            }
            catch (Exception e)
            {
                error = e.Message;
            }
            return cat;
        }
    }
}