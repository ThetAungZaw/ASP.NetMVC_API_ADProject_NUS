﻿using LUSSISADTeam10Web.API;
using LUSSISADTeam10Web.Constants;
using LUSSISADTeam10Web.Models;
using LUSSISADTeam10Web.Models.APIModels;
using LUSSISADTeam10Web.Models.Employee;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;


// Author : Zin Min Htet
namespace LUSSISADTeam10Web.Controllers
{
    [Authorize(Roles = "Employee, DepartmentRep, TempHOD")]
    public class EmployeeController : Controller
    {
        public ActionResult Index()
        {
            string error = "";
            string token = GetToken();
            UserModel um = GetUser();
            List<RequisitionModel> reqs = new List<RequisitionModel>();
            try
            {
                reqs = APIRequisition.GetRequisitionByDepid(um.Deptid, token, out error);

                if (reqs != null)
                {
                    // for dashboard counts

                    ViewBag.ReqCount = reqs.Where(x => x.Status == ConRequisition.Status.PENDING).Count();
                    ViewBag.InProReqCount = reqs.Where(x => x.Status > ConRequisition.Status.PENDING && x.Status < ConRequisition.Status.OUTSTANDINGREQUISITION).Count();
                    ViewBag.OldReqCount = reqs.Where(x => x.Status == ConRequisition.Status.COMPLETED || x.Status == ConRequisition.Status.OUTSTANDINGREQUISITION).Count();
                }
                else
                {
                    ViewBag.ReqCount = 0;
                    ViewBag.InProReqCount = 0;
                    ViewBag.OldReqCount = 0;
                }
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "Error", new { error = ex.Message });
            }
            return View();
        }
        public ActionResult RaiseRequisition()
        {
            string error = "";
            string token = GetToken();
            UserModel um = GetUser();
            DepartmentCollectionPointModel dcpm = new DepartmentCollectionPointModel();
            List<ItemModel> ItemsList = new List<ItemModel>();
            RequisitionViewModel reqvm = new RequisitionViewModel();
            try
            {
                reqvm.Reqdate = DateTime.Now;
                reqvm.Raisedby = um.Userid;
                reqvm.Depid = um.Deptid;
                dcpm = APICollectionPoint.GetActiveDepartmentCollectionPointByDeptID(token, um.Deptid, out error);
                reqvm.Cpid = dcpm.CpID;
                reqvm.Cpname = dcpm.CpName;
                reqvm.Status = ConRequisition.Status.PENDING;
                ItemsList = APIItem.GetAllItems(token, out error);
                ViewBag.ItemsList = ItemsList;
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "Error", new { error = ex.Message });
            }
            return View(reqvm);
        }
        public ActionResult TrackRequisitions()
        {
            string token = GetToken();
            UserModel um = GetUser();

            List<RequisitionModel> reqms = new List<RequisitionModel>();

            try
            {
                reqms = APIRequisition.GetRequisitionByDepid(um.Deptid, token, out string error);

                if (reqms == null)
                {
                    reqms = new List<RequisitionModel>();
                }
                else
                {
                    reqms = reqms.Where(x => x.Status <= ConRequisition.Status.DELIVERED).OrderByDescending(x => x.Reqdate).ToList();
                }

                if (error != "")
                {
                    return RedirectToAction("Index", "Error", new { error });
                }
            }
            catch (Exception ex)
            {
                RedirectToAction("Index", "Error", new { error = ex.Message });
            }

            return View(reqms);
        }
        public ActionResult TrackRequisition(int id)
        {
            string token = GetToken();
            UserModel um = GetUser();
            string error = "";
            RequisitionModel reqm = new RequisitionModel();

            ViewBag.Pending = "btn-danger";
            ViewBag.Preparing = "btn-danger";
            ViewBag.Ready = "btn-danger";
            ViewBag.Collected = "btn-danger";
            ViewBag.Track = "Waiting for Approval from HOD";

            try
            {
                reqm = APIRequisition.GetRequisitionByReqid(id, token, out error);
                if (reqm.Depid != um.Deptid)
                {
                    error = "You don't have authority to view this requisition";
                }
                switch (reqm.Status)
                {
                    case ConRequisition.Status.APPROVED:
                        ViewBag.Pending = "btn-warning";
                        ViewBag.Preparing = "btn-danger";
                        ViewBag.Ready = "btn-danger";
                        ViewBag.Collected = "btn-danger";
                        ViewBag.Track = "Waiting for Approval from Store";
                        break;
                    case ConRequisition.Status.REQUESTPENDING:
                        ViewBag.Pending = "btn-warning";
                        ViewBag.Preparing = "btn-danger";
                        ViewBag.Ready = "btn-danger";
                        ViewBag.Collected = "btn-danger";
                        ViewBag.Track = "Request Pending";
                        break;
                    case ConRequisition.Status.PREPARING:
                        ViewBag.Pending = "btn-success";
                        ViewBag.Preparing = "btn-warning";
                        ViewBag.Ready = "btn-danger";
                        ViewBag.Collected = "btn-danger";
                        ViewBag.Track = "Preparing Items";

                        break;
                    case ConRequisition.Status.DELIVERED:
                        ViewBag.Pending = "btn-success";
                        ViewBag.Preparing = "btn-success";
                        ViewBag.Ready = "btn-warning";
                        ViewBag.Collected = "btn-danger";
                        ViewBag.Track = "Ready to Collect";

                        break;
                    case ConRequisition.Status.OUTSTANDINGREQUISITION:
                        ViewBag.Pending = "btn-success";
                        ViewBag.Preparing = "btn-success";
                        ViewBag.Ready = "btn-success";
                        ViewBag.Collected = "btn-warning";
                        ViewBag.Track = "Completed";

                        break;
                    case ConRequisition.Status.COMPLETED:
                        ViewBag.Pending = "btn-success";
                        ViewBag.Preparing = "btn-success";
                        ViewBag.Ready = "btn-success";
                        ViewBag.Collected = "btn-success";
                        ViewBag.Track = "Completed";
                        break;
                    default:
                        break;
                }

            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "Error", new { error = ex.Message });
            }
            if (error != "")
            {
                return RedirectToAction("Index", "Error", new { error });
            }
            return View(reqm);
        }

        [HttpPost]
        public ActionResult RaiseRequisition(RequisitionViewModel reqvm)
        {
            if (reqvm.Requisitiondetails.Count < 1)
            {
                Session["noti"] = true;
                Session["notitype"] = "error";
                Session["notititle"] = "Raise Requisition Error";
                Session["notimessage"] = "You cannot raise requisition without any items!";
                return RedirectToAction("RaiseRequisition");
            }

            bool duplicateExists = reqvm.Requisitiondetails.GroupBy(n => n.Itemid).Any(g => g.Count() > 1);

            if (duplicateExists)
            {
                Session["noti"] = true;
                Session["notitype"] = "error";
                Session["notititle"] = "Raise Requisition Error";
                Session["notimessage"] = "You cannot raise requisition with duplicate items!";
                return RedirectToAction("RaiseRequisition");
            }

            string token = GetToken();
            UserModel um = GetUser();
            DepartmentCollectionPointModel dcpm = new DepartmentCollectionPointModel();
            List<ItemModel> ItemsList = new List<ItemModel>();
            RequisitionModel reqm = new RequisitionModel();
            List<RequisitionDetailsModel> reqdms = new List<RequisitionDetailsModel>();
            try
            {
                reqm.Reqdate = DateTime.Now;
                reqm.Raisedby = um.Userid;
                if (um.Role == ConUser.Role.TEMPHOD)
                {
                    reqm.Approvedby = um.Userid;
                }
                reqm.Depid = um.Deptid;
                dcpm = APICollectionPoint.GetActiveDepartmentCollectionPointByDeptID(token, um.Deptid, out string error);
                reqm.Cpid = dcpm.CpID;
                reqm.Cpname = dcpm.CpName;
                reqm.Status = ConRequisition.Status.PENDING;
                reqm = APIRequisition.CreateRequisition(reqm, token, out error);

                foreach (var reqd in reqvm.Requisitiondetails)
                {
                    RequisitionDetailsModel reqdm = new RequisitionDetailsModel
                    {
                        Reqid = reqm.Reqid,
                        Itemid = reqd.Itemid,
                        Qty = reqd.Qty
                    };
                    reqdms = APIRequisition.CreateRequisitionDetails(reqdm, token, out error);
                }
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "Error", new { error = ex.Message });
            }
            return RedirectToAction("TrackRequisition", "Employee", new { id = reqm.Reqid });
        }

        #region Utilities
        public string GetToken()
        {
            string token = "";
            token = (string)Session["token"];
            if (string.IsNullOrEmpty(token))
            {
                token = FormsAuthentication.Decrypt(Request.Cookies[FormsAuthentication.FormsCookieName].Value).Name;
                Session["token"] = token;
                UserModel um = APIAccount.GetUserProfile(token, out string error);
                Session["user"] = um;
                Session["role"] = um.Role;
                Session["department"] = um.Deptname;
            }
            return token;
        }
        public UserModel GetUser()
        {
            UserModel um = (UserModel)Session["user"];
            if (um == null)
            {
                GetToken();
                um = (UserModel)Session["user"];
            }
            return um;
        }
        #endregion
    }
}