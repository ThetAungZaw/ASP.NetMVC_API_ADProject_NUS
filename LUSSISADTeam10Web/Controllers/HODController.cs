﻿using LUSSISADTeam10Web.API;
using LUSSISADTeam10Web.Constants;
using LUSSISADTeam10Web.Models;
using LUSSISADTeam10Web.Models.APIModels;
using LUSSISADTeam10Web.Models.Common;
using LUSSISADTeam10Web.Models.HOD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;


// Author: Zin Min Htet | Htet Wai Yan | Hsu Yee Phyo | Aung Myo
namespace LUSSISADTeam10Web.Controllers
{
    public class HODController : Controller
    {
        #region Author : Zin Min Htet

        [Authorize(Roles = "HOD, TempHOD")]
        public ActionResult Index()
        {
            string error = "";
            string token = GetToken();
            UserModel um = GetUser();

            List<RequisitionModel> reqs = new List<RequisitionModel>();
            UserModel CurrentRep = new UserModel();
            DepartmentCollectionPointModel CurrentCP = new DepartmentCollectionPointModel();
            DelegationModel CurrentTemp = new DelegationModel();
            UserModel CurrentTempUser = new UserModel();
            try
            {

                reqs = APIRequisition.GetRequisitionByStatus(ConRequisition.Status.PENDING, token, out error);
                ViewBag.ReqCount = 0;
                ViewBag.ReqCount = reqs.Where(x => x.Depid == um.Deptid).Count();
                ViewBag.DelegationType = "Temporary HOD";

                CurrentRep = APIUser.GetUserByRoleAndDeptID(ConUser.Role.DEPARTMENTREP, um.Deptid, token, out error).FirstOrDefault();
                ViewBag.RepName = CurrentRep.Fullname;
                if (ViewBag.RepName == null)
                {
                    ViewBag.RepName = "None";
                }


                CurrentCP = APICollectionPoint.GetActiveDepartmentCollectionPointByDeptID(token, um.Deptid, out error);
                ViewBag.CollectionPoint = CurrentCP.CpName;
                if (ViewBag.CollectionPoint == null)
                {
                    ViewBag.CollectionPoint = "None";
                }

                CurrentTemp = APIDelegation.GetPreviousDelegationByDepid(token, um.Deptid, out error);
                if (CurrentTemp.Delid != 0)
                {
                    CurrentTempUser = APIUser.GetUserByUserID(CurrentTemp.Userid, token, out error);
                    ViewBag.TempHOD = CurrentTempUser.Fullname;
                    ViewBag.TempDate = CurrentTemp.Startdate.Value.ToShortDateString() + " - " + CurrentTemp.Enddate.Value.ToShortDateString();
                    if (CurrentTemp.Startdate <= DateTime.Today && DateTime.Today <= CurrentTemp.Enddate)
                    {
                        ViewBag.DelegationType = "Current Temporary HOD";
                    }
                    else
                    {
                        ViewBag.DelegationType = "Upcoming Temporary HOD";
                    }
                }
                if (CurrentTemp.Delid == 0 || ViewBag.TempHOD == null)
                {
                    ViewBag.DelegationType = "Temporary HOD";
                    ViewBag.TempHOD = "None";
                    ViewBag.TempDate = "-";
                }
                return View();
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "Error", new { error = ex.Message });

            }
        }

        [Authorize(Roles = "HOD")]
        public ActionResult CancelCollectionPoint(int id)
        {
            string token = GetToken();
            UserModel um = GetUser();
            DepartmentCollectionPointModel dcpm = new DepartmentCollectionPointModel();
            try
            {
                dcpm = APICollectionPoint.GetDepartmentCollectionPointByDcpid(token, id, out string error);
                dcpm.Status = ConDepartmentCollectionPoint.Status.INACTIVE;
                dcpm = APICollectionPoint.RejectDepartmentCollectionPoint(token, dcpm, out error);
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "Error", new { error = ex.Message });
            }
            return RedirectToAction("CollectionPoint");
        }

        [Authorize(Roles = "HOD, TempHOD")]
        public ActionResult RequisitionsList()
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
                    reqms = reqms.Where(p => p.Status < ConRequisition.Status.COMPLETED).OrderByDescending(x => x.Reqdate).ToList();
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

        [Authorize(Roles = "HOD, TempHOD")]
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
            ViewBag.Track = "";

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
                        ViewBag.Track = "Request Pending";
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

        [Authorize(Roles = "HOD, TempHOD, Employee, DepartmentRep")]
        public ActionResult RequisitionDetail(int id)
        {
            string token = GetToken();
            UserModel um = GetUser();
            RequisitionModel reqm = new RequisitionModel();

            try
            {
                reqm = APIRequisition.GetRequisitionByReqid(id, token, out string error);
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "Error", new { error = ex.Message });
            }
            return View(reqm);
        }

        [Authorize(Roles = "HOD, TempHOD")]
        public ActionResult ApproveRequisition(int id)
        {
            string token = GetToken();
            UserModel um = GetUser();
            RequisitionModel reqm = new RequisitionModel();
            ViewBag.RequisitionModel = reqm;
            ApproveRequisitionViewModel viewmodel = new ApproveRequisitionViewModel();

            try
            {
                reqm = APIRequisition.GetRequisitionByReqid(id, token, out string error);

                if (reqm.Status == ConRequisition.Status.APPROVED)
                {
                    Session["noti"] = true;
                    Session["notitype"] = "error";
                    Session["notititle"] = "Already Approved Requisiton!";
                    Session["notimessage"] = "This requisition has already been approved!";
                    return RedirectToAction("Index", "Home");
                }
                else if (reqm.Status == ConRequisition.Status.REJECTED)
                {
                    Session["noti"] = true;
                    Session["notitype"] = "error";
                    Session["notititle"] = "Already Approved Requisiton!";
                    Session["notimessage"] = "This requisition has already been approved!";
                    return RedirectToAction("Index", "Home");
                }

                ViewBag.RequisitionModel = reqm;
                viewmodel.ReqID = reqm.Reqid;
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "Error", new { error = ex.Message });
            }
            return View(viewmodel);
        }

        [Authorize(Roles = "HOD")]
        [HttpPost]
        public ActionResult CollectionPoint(int id)
        {
            string token = GetToken();
            UserModel um = GetUser();
            CollectionPointModel cpm = new CollectionPointModel();
            DepartmentCollectionPointModel dcpm = new DepartmentCollectionPointModel();
            try
            {
                cpm = APICollectionPoint.GetCollectionPointBycpid(token, id, out string error);
                dcpm.CpID = cpm.Cpid;
                dcpm.DeptID = um.Deptid;
                dcpm = APICollectionPoint.CreateDepartmentCollectionPoint(token, dcpm, out error);
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "Error", new { error = ex.Message });
            }
            return RedirectToAction("CollectionPoint");
        }

        [Authorize(Roles = "HOD, TempHOD")]
        [HttpPost]
        public ActionResult ApproveRequisition(ApproveRequisitionViewModel viewmodel)
        {
            string token = GetToken();
            UserModel um = GetUser();
            RequisitionModel reqm = new RequisitionModel();
            NotificationModel nom = new NotificationModel();


            try
            {
                reqm = APIRequisition.GetRequisitionByReqid(viewmodel.ReqID, token, out string error);
                reqm.Status = ConRequisition.Status.APPROVED;
                reqm.Approvedby = um.Userid;
                if (!viewmodel.Approve)
                {
                    reqm.Status = ConRequisition.Status.REJECTED;
                    nom.Deptid = reqm.Depid;
                    nom.Role = ConUser.Role.EMPLOYEE;
                    nom.Title = "Requisition Rejected";
                    nom.NotiType = ConNotification.NotiType.RejectedRequistion;
                    nom.ResID = reqm.Reqid;
                    nom.Remark = "The new requisition has been rejected by the HOD with remark : " + viewmodel.Remark;
                    nom = APINotification.CreateNoti(token, nom, out error);

                    nom.Deptid = reqm.Depid;
                    nom.Role = ConUser.Role.DEPARTMENTREP;
                    nom.Title = "Requisition Rejected";
                    nom.NotiType = ConNotification.NotiType.RejectedRequistion;
                    nom.ResID = reqm.Reqid;
                    nom.Remark = "The new requisition has been rejected by the HOD with remark : " + viewmodel.Remark;
                    nom = APINotification.CreateNoti(token, nom, out error);
                }

                reqm = APIRequisition.UpdateRequisition(reqm, token, out error);


                Session["noti"] = true;
                Session["notitype"] = "success";

                if (viewmodel.Approve)
                {
                    Session["notititle"] = "Requisition Approval";
                    Session["notimessage"] = "Requisiton is now approved!";
                    return RedirectToAction("TrackRequisition", new { id = reqm.Reqid });
                }
                Session["notititle"] = "Requisition Rejection";
                Session["notimessage"] = "Requisiton is rejected!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "Error", new { error = ex.Message });
            }
        }

        #endregion

        #region Author : Aung Myo

        [Authorize(Roles = "HOD")]
        public ActionResult SearchPreviousDelegation()
        {

            string token = GetToken();
            UserModel um = GetUser();

            DelegationModel reqms = new DelegationModel();
            EditDelegationViewModel viewmodel = new EditDelegationViewModel();
            UserModel DelegatedUser = new UserModel();
            try
            {
                reqms = APIDelegation.GetPreviousDelegationByDepid(token, um.Deptid, out string error);
                ViewBag.Userid = reqms.Userid;
                ViewBag.name = reqms.Username;
                ViewBag.StartDate = reqms.Startdate;
                ViewBag.Enddate = reqms.Enddate;
                ViewBag.Deleid = reqms.Delid;
                if (reqms.Userid == 0 || reqms == null)
                {
                    ViewBag.name = "";
                }
                else
                {
                    DelegatedUser = APIUser.GetUserByUserID(reqms.Userid, token, out error);
                    if (DelegatedUser != null && DelegatedUser.Userid != 0)
                    {
                        ViewBag.name = DelegatedUser.Fullname;
                    }

                }
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "Error", new { error = ex.Message });
            }

            return View(viewmodel);
        }

        [Authorize(Roles = "HOD")]
        public JsonResult CancelDelegation(int id)
        {
            string token = GetToken();
            UserModel um = GetUser();
            bool result = false;
            if (id != 0)
            {
                DelegationModel dm = APIDelegation.GetDelegationByDeleid(token, id, out string error);
                DelegationModel dm1 = APIDelegation.CancelDelegation(token, dm, out string cancelerror);
                result = true;
            }

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [Authorize(Roles = "HOD")]
        public ActionResult CreateDelegationList()
        {

            string token = GetToken();
            UserModel um = GetUser();
            List<UserModel> newum = new List<UserModel>();
            CreateDelegationViewModel viewModel = new CreateDelegationViewModel();
            try
            {
                newum = APIUser.GetUsersForHOD(um.Deptid, token, out string error);
                ViewBag.userlist = newum;

                if (error != "")
                {
                    return RedirectToAction("Index", "Error", new { error });
                }
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "Error", new { error = ex.Message });
            }

            return View(viewModel);

        }

        [Authorize(Roles = "HOD")]
        public ActionResult AssignDepRep()
        {
            string token = GetToken();
            UserModel um = GetUser();
            List<UserModel> newum = new List<UserModel>();

            AssignDepRepViewModel viewModel = new AssignDepRepViewModel();
            try
            {
                newum = APIUser.GetAssignRepUserList(token, um.Deptid, out string error);
                ViewBag.userlist = newum;
                List<UserModel> um23 = APIUser.GetUserByRoleAndDeptID(6, um.Deptid, token, out string depreperror);
                foreach (UserModel um1 in um23)
                {
                    ViewBag.assignedrep = um1.Fullname;
                }
                if (error != "")
                {
                    return RedirectToAction("Index", "Error", new { error });
                }
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "Error", new { error = ex.Message });
            }

            return View(viewModel);


        }

        [HttpPost]
        [Authorize(Roles = "HOD")]
        public ActionResult CreateDelegationList(CreateDelegationViewModel viewmodel, int userid)
        {

            string token = GetToken();
            UserModel um = GetUser();

            DelegationModel dm = new DelegationModel();


            try
            {
                if (viewmodel != null)
                {
                    viewmodel.assignedby = um.Userid;
                    dm.Userid = userid;
                    dm.Enddate = (DateTime)viewmodel.EndDate;
                    dm.Startdate = (DateTime)viewmodel.StartDate;
                    dm.AssignedbyId = viewmodel.assignedby;
                    dm = APIDelegation.CreateDelegation(token, dm, out string error);

                }
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "Error", new { error = ex.Message });
            }
            Session["noti"] = true;
            Session["notitype"] = "success";
            Session["notititle"] = "Delegation";
            Session["notimessage"] = dm.Username + " is Delegated as Head of Department";
            return RedirectToAction("SearchPreviousDelegation");
        }

        [Authorize(Roles = "HOD")]
        [HttpPost]
        public ActionResult AssignDepRep(AssignDepRepViewModel viewmodel, int userid = 0)
        {
            if (userid == 0)
            {
                Session["noti"] = true;
                Session["notitype"] = "error";
                Session["notititle"] = "Assign Department Representative";
                Session["notimessage"] = "Please select one employee!";
                return RedirectToAction("AssignDepRep");
            }
            string token = GetToken();
            UserModel um = GetUser();
            UserModel upum = new UserModel();

            try
            {
                if (viewmodel != null)
                {

                    upum = APIUser.AssignDepRep(token, userid, out string error);
                }
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "Error", new { error = ex.Message });
            }
            Session["noti"] = true;
            Session["notitype"] = "success";
            Session["notititle"] = "Assign Department Representative";
            Session["notimessage"] = upum.Fullname + " is assigned as Department Representative";
            return RedirectToAction("AssignDepRep");

        }

        [Authorize(Roles = "HOD")]

        [HttpPost]
        public ActionResult SearchPreviousDelegation(EditDelegationViewModel viewmodel, int id)
        {

            string token = GetToken();
            UserModel um = GetUser();
            DelegationModel um1 = new DelegationModel();

            try
            {
                if (viewmodel != null)
                {
                    viewmodel.assignedby = um.Userid;

                    um1.Delid = id;
                    DelegationModel um2 = APIDelegation.GetDelegationByDeleid(token, id, out string delerror);
                    um1.Startdate = um2.Startdate;
                    um1.Enddate = viewmodel.EndDate;
                    um1.Userid = um2.Userid;
                    um1.AssignedbyId = um.Userid;
                    um1.Active = ConDelegation.Active.ACTIVE;
                    APIDelegation.UpdateDelegation(token, um1, out string error);

                }
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "Error", new { error = ex.Message });
            }
            Session["noti"] = true;
            Session["notitype"] = "success";
            Session["notititle"] = "Update Delegation";
            Session["notimessage"] = "Delegation is updated successfully";

            return RedirectToAction("SearchPreviousDelegation");
        }
        #endregion

        #region Author : Htet Wai Yan
        [HttpPost]
        public JsonResult GetChartData()
        {
            string error = "";
            string token = GetToken();
            UserModel um = GetUser();
            var result = APIReport.GetFrequentItemsHod(token, um.Deptid, out error);
            return Json(new
            {
                labels = result.Select(x => x.description).ToArray(),
                data = result.Select(x => x.Quantity).ToArray()
            }, JsonRequestBehavior.AllowGet);
        }
        [Authorize(Roles = "HOD")]
        public ActionResult CollectionPoint()
        {
            string token = GetToken();
            UserModel um = GetUser();
            DepartmentCollectionPointModel dcpm = new DepartmentCollectionPointModel();
            ViewBag.PendingCPR = false;
            List<CodeValue> CollectionPointsList = new List<CodeValue>();
            List<CollectionPointModel> cpms = new List<CollectionPointModel>();
            List<DepartmentCollectionPointModel> dcpms = new List<DepartmentCollectionPointModel>();

            try
            {
                // to show active collection point
                dcpms = APICollectionPoint.GetDepartmentCollectionPointByStatus(token, ConDepartmentCollectionPoint.Status.PENDING, out string error);

                dcpm = APICollectionPoint.GetActiveDepartmentCollectionPointByDeptID(token, um.Deptid, out error);
                ViewBag.ActiveCollectionPoint = dcpm.CpName;

                CollectionPointModel current =
                    APICollectionPoint.GetCollectionPointBycpid(token, dcpm.CpID, out error);

                ViewBag.Latitude = current.Latitude;
                ViewBag.Longitude = current.Longitude;

                // to show pending list if exists
                dcpms = dcpms.Where(p => p.DeptID == um.Deptid).ToList();
                ViewBag.PendingCollectionPoints = dcpms;
                if (dcpms.Count > 0)
                    ViewBag.PendingCPR = true;


                // for radio button 
                cpms = APICollectionPoint.GetAllCollectionPoints(token, out error);

                ViewBag.CollectionPointsList = cpms;

            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "Error", new { error = ex.Message });
            }


            return View(cpms);
        }
        #endregion

        #region Author : Hsu Yee Phyo

        [Authorize(Roles = "HOD, TempHOD, Employee, DepartmentRep")]
        public ActionResult OrderHistory()
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
                    reqms = reqms.Where(p => p.Status == ConRequisition.Status.COMPLETED).OrderByDescending(x => x.Reqdate).ToList();
                }


                if (error != "")
                {
                    return RedirectToAction("Index", "Error", new { error });
                }
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "Error", new { error = ex.Message });
            }

            return View(reqms);
        }

        #endregion


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
