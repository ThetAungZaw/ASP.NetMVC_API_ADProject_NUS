﻿using LUSSISADTeam10Web.Models.APIModels;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;

// Author : Khin Yadana Phyo
namespace LUSSISADTeam10Web.API
{
    public class APIDepartment
    {
        public static List<DepartmentModel> GetAllDepartments(string token, out string error)
        {
            string url = APIHelper.Baseurl + "/departments/";
            List<DepartmentModel> dms = APIHelper.Execute<List<DepartmentModel>>(token, url, out error);
            return dms;
        }
        public static DepartmentModel GetDepartmentByDeptid(string token, int deptid, out string error)
        {
            string url = APIHelper.Baseurl + "/department/" + deptid;
            DepartmentModel dm =  APIHelper.Execute<DepartmentModel>(token, url, out error);
            return dm;
        }
        public static DepartmentModel GetDepartmentByUserID(string token, int userid, out string error)
        {
            string url = APIHelper.Baseurl + "/department/user/" + userid;
            DepartmentModel dm = APIHelper.Execute<DepartmentModel>(token, url, out error);
            return dm;
        }
        public static DepartmentModel GetDepartmentByCPID(string token, int cpid, out string error)
        {
            string url = APIHelper.Baseurl + "/department/collectionpoint/" + cpid;
            DepartmentModel dm = APIHelper.Execute<DepartmentModel>(token, url, out error);
            return dm;
        }
        public static DepartmentModel GetDepartmentByReqID(string token, int reqid, out string error)
        {
            string url = APIHelper.Baseurl + "/department/requisition/" + reqid;
            DepartmentModel dm = APIHelper.Execute<DepartmentModel>(token, url, out error);
            return dm;
        }
        public static DepartmentModel CreateDepartment(string token, DepartmentModel dm, out string error)
        {
            error = "";
            string url = APIHelper.Baseurl + "/department/create";
            string objectstring = JsonConvert.SerializeObject(dm);
            dm = APIHelper.Execute<DepartmentModel>(token, objectstring, url, out error);
            return dm;
        }
        public static DepartmentModel UpdateDepartment(string token, DepartmentModel dm, out string error)
        {
            error = "";
            string url = APIHelper.Baseurl + "/department/update";
            string objectstring = JsonConvert.SerializeObject(dm);
            dm = APIHelper.Execute<DepartmentModel>(token, objectstring, url, out error);
            return dm;
        }
    }
}