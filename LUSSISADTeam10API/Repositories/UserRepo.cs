﻿using LUSSISADTeam10API.Constants;
using LUSSISADTeam10API.Models;
using LUSSISADTeam10API.Models.APIModels;
using LUSSISADTeam10API.Models.DBModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

// Authors : Zin Min Htet | Thet Aung Zaw | Khin Yadana Phyo
namespace LUSSISADTeam10API.Repositories
{
    public static class UserRepo
    {
        #region Author : Zin Min Htet

        public static UserModel CovertDBUsertoAPIUser(user user)
        {
            UserModel um = new UserModel(user.userid, user.username, user.email, user.password, user.role, user.fullname, user.deptid, user.department.deptname);
            return um;
        }

        public static UserModel ValidateUser(string username, string password)
        {
            // entites used only by Get Methods
            LUSSISEntities entities = new LUSSISEntities();

            user user = entities.users.Where(e => e.username == username && e.password == password).FirstOrDefault<user>();
            return CovertDBUsertoAPIUser(user);
        }

        public static UserModel GetUserByUserID(int userid)
        {
            LUSSISEntities entities = new LUSSISEntities();

            user user = entities.users.Where(p => p.userid == userid).FirstOrDefault<user>();
            return CovertDBUsertoAPIUser(user);
        }

        public static List<UserModel> GetAllUsers()
        {
            LUSSISEntities entities = new LUSSISEntities();

            List<user> users = entities.users.ToList<user>();
            List<UserModel> ums = new List<UserModel>();
            foreach (user user in users)
            {
                ums.Add(CovertDBUsertoAPIUser(user));
            }
            return ums;
        }

        public static UserModel UpdateUser(UserModel um)
        {

            LUSSISEntities entities = new LUSSISEntities();
            user u = entities.users.Where(p => p.userid == um.Userid).First<user>();
            u.userid = um.Userid;
            u.username = um.Username;
            u.email = um.Email;
            u.password = um.Password;
            u.role = um.Role;
            u.deptid = um.Deptid;
            entities.SaveChanges();
            return CovertDBUsertoAPIUser(u);
        }
        #endregion

        #region Author : Thet Aung Zaw
        public static List<UserModel> GetUserByDeptid(int deptid, out string error)
        {
            LUSSISEntities entities = new LUSSISEntities();
            error = "";

            List<department> dept = new List<department>();
            List<user> ums = new List<user>();
            List<UserModel> urm = new List<UserModel>();
            try
            {
                ums = entities.users.Where(p => p.deptid == deptid).ToList<user>();
                foreach (user u in ums)
                {
                    urm.Add(CovertDBUsertoAPIUser(u));
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
            return urm;

        }


        public static List<UserModel> GetUsersForHOD(int deptid, out string error)
        {
            LUSSISEntities entities = new LUSSISEntities();
            error = "";

            List<department> dept = new List<department>();
            List<user> ums = new List<user>();
            List<UserModel> urm = new List<UserModel>();
            try
            {
                ums = entities.users.Where(p => p.deptid == deptid && p.role != ConUser.Role.HOD && p.role != ConUser.Role.DEPARTMENTREP).ToList<user>();
                foreach (user u in ums)
                {
                    urm.Add(CovertDBUsertoAPIUser(u));
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
            return urm;

        }
        public static List<UserModel> GetAssignRepUserList(int deptid, out string error)
        {
            LUSSISEntities entities = new LUSSISEntities();
            error = "";

            List<department> dept = new List<department>();
            List<user> ums = new List<user>();
            List<UserModel> urm = new List<UserModel>();
            try
            {
                List<user> users = entities.users.Where(x => x.userid != (x.delegations.Where(p => p.userid == x.userid && p.active == ConDelegation.Active.ACTIVE).Select(q => q.userid)).FirstOrDefault()).ToList();
                users = users.Where(x => x.deptid == deptid && x.role == ConUser.Role.EMPLOYEEREP).ToList();


                foreach (user u in users)
                {
                    urm.Add(CovertDBUsertoAPIUser(u));
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
            return urm;

        }

        #endregion

        #region Author : Khin Yadana Phyo


        public static List<UserModel> GetUserByRoleandDeptid(int role, int deptid, out string error)
        {
            error = "";
            List<user> user = new List<user>();
            List<UserModel> usm = new List<UserModel>();
            LUSSISEntities entities = new LUSSISEntities();
            try
            {
                user = entities.users.Where(p => p.role == role && p.deptid == deptid).ToList<user>();
                foreach (user u in user)
                {
                    usm.Add(CovertDBUsertoAPIUser(u));
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
            return usm;


        }

        #endregion

        #region Author : Aung Myo
        public static UserModel AssignDepRep(int id)
        {

            LUSSISEntities entities = new LUSSISEntities();
            user um = entities.users.Where(p => p.userid == id).First<user>();
            user um1 = entities.users.Where(p => p.deptid == um.deptid && p.role == ConUser.Role.DEPARTMENTREP).First<user>();
            um1.role = ConUser.Role.EMPLOYEEREP;
            um.role = ConUser.Role.DEPARTMENTREP;
            entities.SaveChanges();
            UserModel umm = CovertDBUsertoAPIUser(um);

            NotificationModel nom = new NotificationModel();
            nom.Deptid = um.deptid;
            nom.Role = ConUser.Role.DEPARTMENTREP;
            nom.Title = "Department Representative";
            nom.NotiType = ConNotification.NotiType.DeptRepAssigned;
            nom.ResID = um.userid;
            nom.Remark = um.fullname + " has been assigned as a Department Representative!";
            nom = NotificationRepo.CreatNotification(nom, out string error);


            nom.Deptid = um.deptid;
            nom.Role = ConUser.Role.EMPLOYEEREP;
            nom.Title = "Department Representative";
            nom.NotiType = ConNotification.NotiType.DeptRepAssigned;
            nom.ResID = um.userid;
            nom.Remark = um.fullname + " has been assigned as a Department Representative!";
            nom = NotificationRepo.CreatNotification(nom, out error);

            return umm;
        }
        public static UserModel Delegateuser(int userid)
        {

            LUSSISEntities entities = new LUSSISEntities();
            user u = entities.users.Where(p => p.userid == userid).First<user>();
            u.role = ConUser.Role.TEMPHOD;
            entities.SaveChanges();
            return CovertDBUsertoAPIUser(u);
        }
        public static UserModel Canceldelegateuser(int userid)
        {

            LUSSISEntities entities = new LUSSISEntities();
            user u = entities.users.Where(p => p.userid == userid).First<user>();
            u.role = ConUser.Role.EMPLOYEEREP;
            entities.SaveChanges();
            return CovertDBUsertoAPIUser(u);
        }
        #endregion
    }
}