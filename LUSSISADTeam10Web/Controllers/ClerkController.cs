﻿using LUSSISADTeam10Web.API;
using LUSSISADTeam10Web.Constants;
using LUSSISADTeam10Web.Models;
using LUSSISADTeam10Web.Models.APIModels;
using LUSSISADTeam10Web.Models.Clerk;
using LUSSISADTeam10Web.Models.Common;
using LUSSISADTeam10Web.Models.Employee;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Excel = Microsoft.Office.Interop.Excel;

// Authors : Zin Min Htet | Aung Myo | Thet Aung Zaw | Hsu Yee Phyo
namespace LUSSISADTeam10Web.Controllers
{
    public class ClerkController : Controller
    {
        #region Author: Zin Min Htet

        [Authorize(Roles = "Clerk")]
        public ActionResult Index()
        {
            string token = GetToken();
            UserModel um = GetUser();
            string error = "";
            // FOR chart
            List<FrequentlyTop5ItemsModel> reportData = new List<FrequentlyTop5ItemsModel>();

            List<RequisitionModel> reqs = new List<RequisitionModel>();

            List<OutstandingReqModel> outs = new List<OutstandingReqModel>();

            List<InventoryDetailModel> invs = new List<InventoryDetailModel>();


            try
            {
                reportData = APIReport.FrequentlyItemList(token, out error);

                reqs = APIRequisition.GetRequisitionByStatus(ConRequisition.Status.APPROVED, token, out error);
                ViewBag.ReqCount = reqs.Count;

                outs = APIOutstandingReq.GetAllOutReqs(token, out error);
                ViewBag.OutCount = outs.Where(x => x.Status == ConOutstandingsRequisition.Status.PENDING).Count();

                reqs = APIRequisition.GetRequisitionByStatus(ConRequisition.Status.PREPARING, token, out error);
                ViewBag.DisCount = reqs.Count;

                invs = APIInventory.GetAllInventoryDetails(token, out error);
                ViewBag.RestockCount = invs.Where(x => x.RecommendedOrderQty > 0).Count();

                return View("Index", reportData);
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "Error", new { error = ex.Message });
            }
        }



        [Authorize(Roles = "Clerk")]
        public ActionResult RequisitionsComplete()
        {
            string error = "";
            string token = GetToken();
            List<RequisitionModel> reqms = new List<RequisitionModel>();

            try
            {
                reqms = APIRequisition.GetAllRequisition(token, out error);

                reqms = reqms.Where(x => x.Status == ConRequisition.Status.DELIVERED || x.Status == ConRequisition.Status.COMPLETED || x.Status == ConRequisition.Status.OUTSTANDINGREQUISITION).ToList();

                if (reqms != null)
                {
                    reqms = reqms.OrderByDescending(x => x.Reqdate).ToList();
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



        [Authorize(Roles = "Clerk")]
        public ActionResult Requisition()
        {
            string token = GetToken();
            UserModel um = GetUser();
            string error = "";

            List<RequisitionModel> reqms = new List<RequisitionModel>();
            try
            {
                reqms = APIRequisition.GetRequisitionByStatus(ConRequisition.Status.APPROVED, token, out error);
                ViewBag.ReqCount = reqms.Count;
                if (reqms.Count > 0)
                {
                    reqms = reqms.OrderBy(x => x.Reqdate).ToList();
                }
                ViewBag.Requisitions = reqms;

                return View(new Models.Clerk.RequisitionViewModel());
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "Error", new { error = ex.Message });
            }
        }

        [Authorize(Roles = "Clerk")]
        [HttpPost]
        public JsonResult ApproveAllRequisitons(int[] reqids)
        {
            string token = GetToken();
            UserModel um = GetUser();
            string error = "";
            bool ResultSuccess = false;
            List<RequisitionModel> reqms = new List<RequisitionModel>();
            DisbursementModel dis = new DisbursementModel();
            OutstandingReqModel outr = new OutstandingReqModel();
            bool IsNeededOutstanding = false;

            try
            {
                foreach (int i in reqids)
                {
                    RequisitionModel req = new RequisitionModel();
                    req = APIRequisition.GetRequisitionByReqid(i, token, out error);
                    if (req != null)
                    {
                        dis.Reqid = req.Reqid;
                        dis.Ackby = um.Userid;
                        dis = APIDisbursement.Createdisbursement(dis, token, out error);

                        foreach (RequisitionDetailsModel reqd in req.Requisitiondetails)
                        {
                            if (reqd.Stock < reqd.Qty)
                            {
                                IsNeededOutstanding = true;
                            }
                            else
                            {
                                IsNeededOutstanding = false;
                            }
                        }

                        if (IsNeededOutstanding)
                        {
                            outr.ReqId = req.Reqid;
                            outr.Reason = "Not Enough Stock";
                            outr.Status = ConOutstandingsRequisition.Status.PENDING;
                            outr = APIOutstandingReq.CreateOutReq(outr, token, out error);
                        }

                        foreach (RequisitionDetailsModel reqd in req.Requisitiondetails)
                        {
                            DisbursementDetailsModel disdm = new DisbursementDetailsModel
                            {
                                Disid = dis.Disid,
                                Itemid = reqd.Itemid
                            };
                            if (reqd.Qty > reqd.Stock)
                            {
                                disdm.Qty = reqd.Stock;
                            }
                            else
                            {
                                disdm.Qty = reqd.Qty;
                            }
                            disdm = APIDisbursement.CreateDisbursementDetails(disdm, token, out error);


                            if (reqd.Qty > reqd.Stock)
                            {
                                OutstandingReqDetailModel outreq = new OutstandingReqDetailModel();
                                outreq.OutReqId = outr.OutReqId;
                                outreq.ItemId = reqd.Itemid;
                                outreq.Qty = reqd.Qty - disdm.Qty;
                                outreq = APIOutstandingReq.CreateOutReqDetail(outreq, token, out error);
                            }
                        }

                    }
                    req = APIRequisition.UpdateRequisitionStatusToPending(req, token, out error);

                    ResultSuccess = true;
                }
            }
            catch (Exception ex)
            {
                var errorm = ex.Message;
            }

            return Json(ResultSuccess, JsonRequestBehavior.AllowGet);
        }

        [Authorize(Roles = "Clerk")]
        public ActionResult RequisitionDetail(int id)
        {
            string token = GetToken();
            UserModel um = GetUser();
            string error = "";


            RequisitionModel reqm = new RequisitionModel();
            try
            {
                reqm = APIRequisition.GetRequisitionByReqid(id, token, out error);

                if (reqm.Status != ConRequisition.Status.APPROVED)
                {
                    return RedirectToAction("Requisition");
                }

                ViewBag.Requisition = reqm;
                ProcessRequisitionViewModel vm = new ProcessRequisitionViewModel
                {
                    ReqID = reqm.Reqid
                };
                vm.ReqItems = new List<ReqItem>();

                foreach (RequisitionDetailsModel rd in reqm.Requisitiondetails)
                {
                    ReqItem ri = new ReqItem
                    {
                        ItemID = rd.Itemid,
                        ItemName = rd.Itemname,
                        CategoryName = rd.CategoryName,
                        Qty = rd.Qty,
                        Stock = rd.Stock,
                        UOM = rd.UOM
                    };
                    vm.ReqItems.Add(ri);
                }
                return View(vm);

            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "Error", new { error = ex.Message });
            }
        }

        [Authorize(Roles = "Clerk")]
        [HttpPost]
        public ActionResult RequisitionDetail(ProcessRequisitionViewModel viewmodel, List<int> itemids, List<int> ApproveQtys)
        {
            int count = 0;
            string error = "";
            string token = GetToken();
            UserModel um = GetUser();
            bool IsNeededOutstanding = false;
            OutstandingReqModel outr = new OutstandingReqModel();
            RequisitionModel reqm = new RequisitionModel();

            try
            {
                reqm = APIRequisition.GetRequisitionByReqid(viewmodel.ReqID, token, out error);
                List<RequisitionDetailsModel> reqdms = reqm.Requisitiondetails;
                viewmodel.ReqItems = new List<ReqItem>();
                foreach (int itemid in itemids)
                {
                    ReqItem ri = new ReqItem();
                    RequisitionDetailsModel reqdm = reqdms.Where(p => p.Itemid == itemid).FirstOrDefault();
                    ri.ItemID = itemid;
                    ri.ApproveQty = ApproveQtys[count];
                    ri.Qty = reqdm.Qty;
                    ri.Stock = reqdm.Stock;
                    ri.UOM = reqdm.UOM;
                    ri.ItemName = reqdm.Itemname;
                    ri.CategoryName = reqdm.CategoryName;
                    if ((ri.Qty - ri.ApproveQty) > 0)
                    {
                        IsNeededOutstanding = true;
                    }
                    viewmodel.ReqItems.Add(ri);
                    count++;
                }

                DisbursementModel dis = new DisbursementModel();
                dis.Reqid = viewmodel.ReqID;
                dis.Ackby = um.Userid;
                dis = APIDisbursement.Createdisbursement(dis, token, out error);

                if (IsNeededOutstanding)
                {
                    outr.ReqId = viewmodel.ReqID;
                    outr.Reason = "Not Enough Stock";
                    outr.Status = ConOutstandingsRequisition.Status.PENDING;
                    outr = APIOutstandingReq.CreateOutReq(outr, token, out error);
                }

                foreach (ReqItem ri in viewmodel.ReqItems)
                {
                    DisbursementDetailsModel disdm = new DisbursementDetailsModel();
                    disdm.Disid = dis.Disid;
                    disdm.Itemid = ri.ItemID;
                    disdm.Qty = ri.ApproveQty;
                    disdm = APIDisbursement.CreateDisbursementDetails(disdm, token, out error);
                    if (ri.Qty > ri.ApproveQty)
                    {
                        OutstandingReqDetailModel outreq = new OutstandingReqDetailModel();
                        outreq.OutReqId = outr.OutReqId;
                        outreq.ItemId = ri.ItemID;
                        outreq.Qty = ri.Qty - ri.ApproveQty;
                        outreq = APIOutstandingReq.CreateOutReqDetail(outreq, token, out error);
                    }
                }
                reqm = APIRequisition.UpdateRequisitionStatusToPending(reqm, token, out error);

                Session["noti"] = true;
                Session["notitype"] = "success";
                Session["notititle"] = "Requisition";
                Session["notimessage"] = "Reqisition is approved";

                return RedirectToAction("StationaryRetrievalForm");
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "Error", new
                {
                    error = ex.Message
                });
            }
        }

        [Authorize(Roles = "Clerk")]
        public ActionResult Outstanding()
        {
            string token = GetToken();
            UserModel um = GetUser();
            string error = "";
            List<OutstandingReqModel> outrm = new List<OutstandingReqModel>();
            List<OutReqViewModel> outreqvms = new List<OutReqViewModel>();

            try
            {
                outrm = APIOutstandingReq.GetAllOutReqs(token, out error);

                foreach (OutstandingReqModel outr in outrm)
                {
                    OutReqViewModel outreqvm = new OutReqViewModel();
                    RequisitionModel reqm = new RequisitionModel();
                    reqm = APIRequisition.GetRequisitionByReqid(outr.ReqId, token, out error);
                    outreqvm.ReqId = outr.ReqId;
                    outreqvm.DeptId = reqm.Depid;
                    outreqvm.DeptName = reqm.Depname;
                    outreqvm.OutReqId = outr.OutReqId;
                    outreqvm.ReqDate = reqm.Reqdate ?? DateTime.Now;
                    outreqvm.Status = outr.Status;
                    outreqvm.Reason = outr.Reason;
                    outreqvm.CanFullFill = APIOutstandingReq.CheckInventoryStock(token, outreqvm.OutReqId, out error);
                    outreqvm.OutReqDetails = outr.OutReqDetails;
                    if (reqm.Status >= ConRequisition.Status.REQUESTPENDING)
                    {
                        outreqvms.Add(outreqvm);
                    }
                }

                if (outreqvms != null)
                {
                    outreqvms = outreqvms.Where(x => x.Status != ConOutstandingsRequisition.Status.COMPLETE).OrderBy(x => x.Status).ToList();
                }


                ViewBag.Outstandings = outreqvms;

                return View();
            }

            catch (Exception ex)
            {
                return RedirectToAction("Index", "Error", new
                {
                    error = ex.Message
                });
            }
        }

        [Authorize(Roles = "Clerk")]
        public ActionResult OutstandingDetail(int id)
        {
            string error = "";
            string token = GetToken();
            UserModel um = GetUser();
            RequisitionModel reqm = new RequisitionModel();
            OutReqViewModel outreqvm = new OutReqViewModel();
            OutstandingReqModel outr = new OutstandingReqModel();
            DepartmentCollectionPointModel dcpm = new DepartmentCollectionPointModel();

            try
            {
                outr = APIOutstandingReq.GetOutReqByReqId(token, id, out error);
                reqm = APIRequisition.GetRequisitionByReqid(id, token, out error);
                dcpm = APICollectionPoint.GetActiveDepartmentCollectionPointByDeptID(token, reqm.Depid, out error);
                outreqvm.CanFullFill = APIOutstandingReq.CheckInventoryStock(token, outr.OutReqId, out error);
                outreqvm.ReqId = outr.ReqId;
                outreqvm.DeptId = reqm.Depid;
                outreqvm.DeptName = reqm.Depname;
                outreqvm.OutReqId = outr.OutReqId;
                outreqvm.ReqDate = reqm.Reqdate ?? DateTime.Now;
                outreqvm.Status = outr.Status;
                outreqvm.Reason = outr.Reason;
                outreqvm.OutReqDetails = outr.OutReqDetails;
                ViewBag.ColectionPoint = dcpm.CpName;
                ViewBag.LockerName = "Z" + reqm.Depid;
                return View(outreqvm);
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "Error", new
                {
                    error = ex.Message
                });
            }

        }

        [Authorize(Roles = "Clerk")]
        public ActionResult ProcessOutstanding(int id)
        {
            string error = "";
            string token = GetToken();
            UserModel um = GetUser();
            RequisitionModel reqm = new RequisitionModel();
            OutstandingReqModel outr = new OutstandingReqModel();

            try
            {
                outr = APIOutstandingReq.GetOutReqByReqId(token, id, out error);
                reqm = APIRequisition.GetRequisitionByReqid(id, token, out error);
                bool CanFullFill = APIOutstandingReq.CheckInventoryStock(token, outr.OutReqId, out error);

                if (reqm.Status == ConRequisition.Status.COMPLETED ||
                    outr.Status != ConOutstandingsRequisition.Status.PENDING || CanFullFill == false)
                {
                    return RedirectToAction("Outstanding");
                }

                outr.Status = ConOutstandingsRequisition.Status.DELIVERED;

                outr = APIOutstandingReq.UpdateOutReq(outr, token, out error);


                NotificationModel nom = new NotificationModel();
                nom.Datetime = DateTime.Now;
                nom.Deptid = reqm.Depid;
                nom.Remark = "The Outstanding Items with Requisition ID (" + reqm.Reqid + ") is now ready to collect";
                nom.Role = ConUser.Role.DEPARTMENTREP;
                nom.Title = "Outstanding Items Ready to Collect";
                nom.NotiType = ConNotification.NotiType.OutstandingItemsReadyToCollect;
                nom.ResID = outr.OutReqId;
                nom = APINotification.CreateNoti(token, nom, out error);


                return RedirectToAction("OutstandingDetail", new { id = outr.ReqId });
            }


            catch (Exception ex)
            {
                return RedirectToAction("Index", "Error", new
                {
                    error = ex.Message
                });
            }
        }

        [Authorize(Roles = "Clerk")]
        public JsonResult UpdateToPreparing()
        {
            string token = GetToken();
            UserModel um = GetUser();
            string error = "";

            List<RequisitionWithDisbursementModel> reqdisms = new List<RequisitionWithDisbursementModel>();

            reqdisms = APIRequisition.UpdateAllRequistionRequestStatusToPreparing(token, out error);

            bool ResultSuccess = false;

            if (error == "" || reqdisms != null)
            {
                ResultSuccess = true;
            }
            return Json(ResultSuccess, JsonRequestBehavior.AllowGet);
        }

        [Authorize(Roles = "Clerk")]
        public ActionResult DisbursementLists()
        {
            string token = GetToken();
            UserModel um = GetUser();
            string error = "";
            ViewBag.ShowItems = false;

            List<RequisitionWithDisbursementModel> reqdisms = new List<RequisitionWithDisbursementModel>();
            try
            {
                reqdisms = APIRequisition.GetRequisitionWithPreparingStatus(token, out error);

                List<string> CollectionPointNames = new List<string>();
                CollectionPointNames = reqdisms.Select(x => x.Cpname).Distinct().ToList();
                ViewBag.CollectionPointNames = CollectionPointNames;
                if (CollectionPointNames.Count > 0)
                {
                    ViewBag.ShowItems = true;
                }

                return View(reqdisms);
            }


            catch (Exception ex)
            {
                return RedirectToAction("Index", "Error", new
                {
                    error = ex.Message
                });
            }
        }

        [Authorize(Roles = "Clerk")]
        public ActionResult ItemDelivered(int id)
        {
            string error = "";
            string token = GetToken();
            UserModel um = GetUser();
            RequisitionModel req = new RequisitionModel();

            try
            {
                req = APIRequisition.GetRequisitionByReqid(id, token, out error);

                if (req.Status != ConRequisition.Status.PREPARING)
                {
                    RedirectToAction("DisbursementLists");
                }

                req.Status = ConRequisition.Status.DELIVERED;
                req = APIRequisition.UpdateRequisition(req, token, out error);

                // add notification here

                return RedirectToAction("DisbursementDetail", new { id = req.Reqid });
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "Error", new
                {
                    error = ex.Message
                });
            }
        }

        [Authorize(Roles = "Clerk")]
        public ActionResult DisbursementDetail(int id)
        {
            string error = "";
            string token = GetToken();
            UserModel um = GetUser();
            RequisitionWithDisbursementModel req = new RequisitionWithDisbursementModel();

            try
            {
                req = APIRequisition.GetRequisitionWithDisbursementByReqID(id, token, out error);
                if (req.Status != ConRequisition.Status.DELIVERED)
                {
                    RedirectToAction("DisbursementLists");
                }
                return View(req);
            }


            catch (Exception ex)
            {
                return RedirectToAction("Index", "Error", new
                {
                    error = ex.Message
                });
            }
        }

        [Authorize(Roles = "Clerk, Manager, Supervisor")]
        public ActionResult PurchaseOrder()
        {
            string error = "";
            string token = GetToken();
            UserModel um = GetUser();

            List<InventoryDetailModel> inendetail = new List<InventoryDetailModel>();
            PurchaseOrderViewModel povm = new PurchaseOrderViewModel();
            List<ItemModel> ItemsList = new List<ItemModel>();
            List<SupplierModel> sups = new List<SupplierModel>();
            List<SupplierItemModel> SupItems = new List<SupplierItemModel>();
            povm.podms = new List<PurchaseOrderDetailViewModel>();
            List<SupplierItemModel> Supitemprices = new List<SupplierItemModel>();
            List<PurchaseOrderDetailViewModel> poview = new List<PurchaseOrderDetailViewModel>();

            try
            {
                inendetail = APIInventory.GetAllInventoryDetails(token, out error);
                SupItems = APISupplier.GetAllSupplierItems(token, out error);

                inendetail = inendetail.Where(p => p.RecommendedOrderQty > 0).ToList();

                if (error != "")
                {
                    return RedirectToAction("Index", "Error", new { error });
                }

                povm.Purchasedby = um.Userid;
                povm.Podate = DateTime.Now;
                povm.Status = ConPurchaseOrder.Status.PENDING;

                ItemsList = APIItem.GetAllActiveSupplierItems(token, out error);
                ViewBag.ItemsList = ItemsList;

                foreach (InventoryDetailModel ivndm in inendetail)
                {
                    PurchaseOrderDetailViewModel podvm = new PurchaseOrderDetailViewModel
                    {
                        Itemid = ivndm.Itemid,
                        ItemDescription = ivndm.ItemDescription,
                        Qty = ivndm.RecommendedOrderQty ?? default(int),
                        Prices = SupItems.Where(x => x.ItemId == ivndm.Itemid).Select(x => x.Price).ToList(),
                        SupplierIDs = SupItems.Where(x => x.ItemId == ivndm.Itemid).Select(x => x.SupId).ToList(),
                        SupplierNames = SupItems.Where(x => x.ItemId == ivndm.Itemid).Select(x => x.SupName).ToList(),
                        LowestPrice = SupItems.Where(x => x.ItemId == ivndm.Itemid).Select(x => x.Price).Min()
                    };
                    poview.Add(podvm);
                }
                ViewBag.poview = poview;

            }
            catch (Exception ex)
            {
                RedirectToAction("Index", "Error", new { error = ex.Message });
            }

            return View(povm);
        }

        [Authorize(Roles = "Clerk, Manager, Supervisor")]
        public ActionResult PurchaseOrders()
        {
            string error = "";
            string token = GetToken();
            UserModel um = GetUser();

            List<PurchaseOrderModel> pom = new List<PurchaseOrderModel>();
            try
            {
                pom = APIPurchaseOrder.GetAllPurchaseOrders(token, out error);

                if (pom != null)
                {
                    pom = pom.OrderByDescending(x => x.Podate).ThenBy(x => x.Status).ToList();
                }
                else
                {
                    pom = new List<PurchaseOrderModel>();
                }

                return View(pom);
            }

            catch (Exception ex)
            {
                return RedirectToAction("Index", "Error", new
                {
                    error = ex.Message
                });
            }
        }

        [Authorize(Roles = "Clerk, Manager, Supervisor")]
        public ActionResult PurchaseOrderDetail(int id)
        {
            string error = "";
            string token = GetToken();
            UserModel um = GetUser();

            PurchaseOrderModel pom = new PurchaseOrderModel();
            try
            {

                pom = APIPurchaseOrder.GetPurchaseOrderByID(token, id, out error);

                if (pom == null || pom.Status == ConPurchaseOrder.Status.PENDING)
                {
                    return RedirectToAction("PurchaseOrders");
                }
                ViewBag.pom = pom;

                PurchaseOrderViewModel povm = new PurchaseOrderViewModel();

                return View(povm);
            }


            catch (Exception ex)
            {
                return RedirectToAction("Index", "Error", new
                {
                    error = ex.Message
                });
            }
        }

        [Authorize(Roles = "Clerk, Manager, Supervisor")]
        public ActionResult ProcessPurchaseOrderDetail(int id)
        {
            string error = "";
            string token = GetToken();
            UserModel um = GetUser();

            PurchaseOrderModel pom = new PurchaseOrderModel();

            try
            {
                pom = APIPurchaseOrder.GetPurchaseOrderByID(token, id, out error);

                if (pom == null || pom.Status != ConPurchaseOrder.Status.PENDING)
                {
                    return RedirectToAction("PurchaseOrders");
                }

                ViewBag.pom = pom;

                PurchaseOrderViewModel povm = new PurchaseOrderViewModel();

                return View(povm);
            }


            catch (Exception ex)
            {
                return RedirectToAction("Index", "Error", new
                {
                    error = ex.Message
                });
            }
        }

        [Authorize(Roles = "Clerk, Manager, Supervisor")]
        public ActionResult CancelPurchaseOrder(int id)
        {
            string error = "";
            string token = GetToken();
            UserModel um = GetUser();

            PurchaseOrderModel pom = new PurchaseOrderModel();

            try
            {
                pom = APIPurchaseOrder.GetPurchaseOrderByID(token, id, out error);

                if (pom == null || pom.Status != ConPurchaseOrder.Status.PENDING)
                {
                    Session["noti"] = true;
                    Session["notitype"] = "error";
                    Session["notititle"] = "Purchase Order";
                    Session["notimessage"] = "The Purchase Order has already been expired!";
                    return RedirectToAction("PurchaseOrders");
                }

                pom.Status = ConPurchaseOrder.Status.CANCELLED;

                pom = APIPurchaseOrder.UpdatePO(pom, token, out error);

                Session["noti"] = true;
                Session["notitype"] = "success";
                Session["notititle"] = "Purchase Order";
                Session["notimessage"] = "The Purchase Order has been cancelled successfully";
                return RedirectToAction("PurchaseOrders");
            }


            catch (Exception ex)
            {
                return RedirectToAction("Index", "Error", new
                {
                    error = ex.Message
                });
            }
        }


        [Authorize(Roles = "Clerk, Manager, Supervisor")]
        [HttpPost]
        public ActionResult ProcessPurchaseOrderDetail(PurchaseOrderViewModel povm)
        {
            string error = "";
            string token = GetToken();
            UserModel um = GetUser();
            PurchaseOrderModel pom = new PurchaseOrderModel();

            try
            {
                List<PurchaseOrderDetailModel> podms = new List<PurchaseOrderDetailModel>();
                if (povm.podms == null)
                {
                    return RedirectToAction("PurchaseOrders");
                }

                pom = APIPurchaseOrder.GetPurchaseOrderByID(token, povm.PoId, out error);

                foreach (PurchaseOrderDetailViewModel podvm in povm.podms)
                {
                    PurchaseOrderDetailModel podm = new PurchaseOrderDetailModel();
                    podm = pom.podms.Where(x => x.Itemid == podvm.Itemid).FirstOrDefault();
                    podm.DelivQty = podvm.DelivQty;
                    podm = APIPurchaseOrder.UpdatePODetail(podm, token, out error);
                }

                // pom = APIPurchaseOrder.GetPurchaseOrderByID(token, povm.PoId, out error);

                pom.Status = ConPurchaseOrder.Status.RECEIVED;

                pom = APIPurchaseOrder.UpdatePOStatusComplete(pom, token, out error);
                Session["noti"] = true;
                Session["notitype"] = "success";
                Session["notititle"] = "Purchase Order Update Success";
                Session["notimessage"] = "The Purchase Order Status has been updated successfully";
                return RedirectToAction("PurchaseOrderDetail", new { id = pom.PoId });
            }


            catch (Exception ex)
            {
                return RedirectToAction("Index", "Error", new
                {
                    error = ex.Message
                });
            }
        }

        [Authorize(Roles = "Clerk, Manager, Supervisor")]

        public PartialViewResult GetSupplierLists(int id)
        {
            string error = "";
            string token = GetToken();
            UserModel um = GetUser();
            List<SupplierItemModel> supitems = new List<SupplierItemModel>();

            supitems = APISupplier.GetAllSupplierItems(token, out error);


            supitems = supitems.Where(x => x.ItemId == id).ToList();

            ViewBag.minsup = supitems.Where(x => x.ItemId == id).Min(x => x.Price);

            ViewBag.supitems = supitems;
            return PartialView();
        }


        [Authorize(Roles = "Clerk, Manager, Supervisor")]
        [HttpPost]
        public ActionResult PurchaseOrder(PurchaseOrderViewModel povm)
        {

            if (povm.podms.Count < 1)
            {
                Session["noti"] = true;
                Session["notitype"] = "error";
                Session["notititle"] = "Purchase Order";
                Session["notimessage"] = "You cannot create purchase order with items!";
                return RedirectToAction("PurchaseOrder");
            }
            string error = "";
            string token = GetToken();
            UserModel um = GetUser();

            List<PurchaseOrderDetailViewModel> podvm = new List<PurchaseOrderDetailViewModel>();
            List<PurchaseOrderModel> POIDs = new List<PurchaseOrderModel>();
            List<SupplierItemModel> SupItems = new List<SupplierItemModel>();

            try
            {
                SupItems = APISupplier.GetAllSupplierItems(token, out error);

                podvm = povm.podms;
                List<int> sups = podvm.Select(x => x.SupplierID).Distinct().ToList();

                foreach (int i in sups)
                {
                    List<PurchaseOrderDetailViewModel> odvm = podvm.Where(x => x.SupplierID == i).ToList();
                    if (odvm.Count > 0)
                    {
                        PurchaseOrderModel pom = new PurchaseOrderModel
                        {
                            Purchasedby = um.Userid,
                            Supid = i,
                            Podate = DateTime.Now,
                            Status = ConPurchaseOrder.Status.PENDING
                        };
                        pom = APIPurchaseOrder.CreatePurchaseOrder(pom, token, out error);
                        foreach (PurchaseOrderDetailViewModel od in odvm)
                        {
                            PurchaseOrderDetailModel podm = new PurchaseOrderDetailModel();
                            podm.PoId = pom.PoId;
                            podm.Itemid = od.Itemid;
                            podm.Qty = od.Qty;
                            podm.DelivQty = od.Qty;
                            podm.Price = SupItems.Where(x => x.ItemId == od.Itemid && x.SupId == od.SupplierID).First().Price;
                            podm = APIPurchaseOrder.CreatePODetail(podm, token, out error);
                        }

                        pom = APIPurchaseOrder.GetPurchaseOrderByID(token, pom.PoId, out error);

                        POIDs.Add(pom);

                        SupplierModel sup = APISupplier.GetSupplierById(pom.Supid, token, out error);
                        bool result = Utilities.Utility.SendPurchaseOrdersPDF(sup, pom);
                    }
                }

                TempData["pos"] = POIDs;


                Session["noti"] = true;
                Session["notitype"] = "success";
                Session["notititle"] = "Purchase Order";
                Session["notimessage"] = "The Purchase Orders has been created and Email with POs has been sent to suppliers successfully";
                return RedirectToAction("PODetails");
            }


            catch (Exception ex)
            {
                return RedirectToAction("Index", "Error", new
                {
                    error = ex.Message
                });
            }
        }

        [Authorize(Roles = "Clerk, Manager, Supervisor")]
        public ActionResult PODetails()
        {
            List<PurchaseOrderModel> pos = new List<PurchaseOrderModel>();

            try
            {
                pos = (List<PurchaseOrderModel>)TempData["pos"];

                if (pos == null)
                {
                    return View("PurchaseOrders");
                }
                return View(pos);
            }


            catch (Exception ex)
            {
                return RedirectToAction("Index", "Error", new
                {
                    error = ex.Message
                });
            }
        }

        [Authorize(Roles = "Clerk")]
        public ActionResult StationaryRetrievalForm()
        {
            string token = GetToken();
            UserModel um = GetUser();

            List<OutstandingItemModel> inendetail = new List<OutstandingItemModel>();
            List<BreakdownByDepartmentModel> bkm = new List<BreakdownByDepartmentModel>();

            List<ShowBD> bkmd = new List<ShowBD>();
            try
            {
                inendetail = APIDisbursement.GetRetriveItemListforClerk(token, out string error);

                bkm = APIDisbursement.GetBreakDown(token, out string errors);


            }




            catch (Exception ex)
            {
                var mes = ex.Message;
            }

            return View(bkm);
        }

        #endregion

        #region Author: Aung Myo
        [Authorize(Roles = "Clerk, Manager, Supervisor")]
        public ActionResult ShowActiveSupplierlist()
        {
            string token = GetToken();
            UserModel um = GetUser();
            List<SupplierModel> sm = new List<SupplierModel>();
            try
            {
                sm = APISupplier.GetSupplierByStatus(ConSupplier.Active.ACTIVE, token, out string error);

                return View(sm);

            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "Error", new { error = ex.Message });
            }

        }

        [Authorize(Roles = "Clerk, Manager, Supervisor")]
        public ActionResult ShowDeActiveSupplierlist()
        {
            string token = GetToken();
            UserModel um = GetUser();
            List<SupplierModel> sm = new List<SupplierModel>();



            try
            {
                sm = APISupplier.GetSupplierByStatus(ConSupplier.Active.INACTIVE, token, out string error);

                return View(sm);

            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "Error", new { error = ex.Message });
            }

        }

        [Authorize(Roles = "Clerk, Manager, Supervisor")]
        public ActionResult SupllierDetails(int id)
        {
            string token = GetToken();
            UserModel um = GetUser();

            SupplierModel sm = new SupplierModel();
            List<SupplierItemModel> smlist = new List<SupplierItemModel>();


            try
            {
                sm = APISupplier.GetSupplierById(id, token, out string supperror);
                ViewBag.suppliername = sm.SupName;
                smlist = APISupplier.GetItemsBySupplierId(id, token, out string error);

                return View(smlist);

            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "Error", new { error = ex.Message });
            }




        }

        [Authorize(Roles = "Clerk, Manager, Supervisor")]
        public JsonResult DeActive(int id)
        {

            string token = GetToken();
            UserModel um = GetUser();
            bool result = false;
            SupplierModel sm = new SupplierModel();
            sm = APISupplier.GetSupplierById(id, token, out string superror);

            try
            {

                APISupplier.DeactivateSupplier(sm, token, out string error);

                result = true;
            }
            catch (Exception)
            {
                //return RedirectToAction("Index", "Error", new { error = ex.Message });
            }
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [Authorize(Roles = "Clerk, Manager, Supervisor")]
        public JsonResult Active(int id)
        {

            string token = GetToken();
            UserModel um = GetUser();
            bool result = false;
            SupplierModel sm = new SupplierModel();
            sm = APISupplier.GetSupplierById(id, token, out string superror);

            try
            {

                APISupplier.ActivateSupplier(sm, token, out string error);

                result = true;
            }
            catch (Exception)
            {
                //return RedirectToAction("Index", "Error", new { error = ex.Message });
            }
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [Authorize(Roles = "Clerk, Manager, Supervisor")]
        public ActionResult CreateSuppandItem()
        {
            return View("CreateSuppandItem");
        }

        [Authorize(Roles = "Clerk, Manager, Supervisor")]
        [HttpPost]
        public ActionResult Csvsupplier(HttpPostedFileBase excelfile)
        {
            string token = GetToken();
            UserModel um = GetUser();
            try
            {
                if (excelfile == null || excelfile.ContentLength == 0)
                {
                    Session["noti"] = true;
                    Session["notitype"] = "error";
                    Session["notititle"] = "Import Error";
                    Session["notimessage"] = "File is Empty ";

                    ViewBag.Error1 = "File is Empty";
                    return View("CreateSuppandItem");
                }

                else
                {


                    if (excelfile.FileName.EndsWith("xls") || excelfile.FileName.EndsWith("xlsx"))
                    {
                        string path = Server.MapPath("~/Content/" + excelfile.FileName);
                        if (System.IO.File.Exists(path))
                            System.IO.File.Delete(path);
                        excelfile.SaveAs(path);
                        // read data from excel file
                        Excel.Application application = new Excel.Application();
                        Excel.Workbook workbook = application.Workbooks.Open(path);
                        Excel.Worksheet worksheet = workbook.ActiveSheet;
                        Excel.Range range = worksheet.UsedRange;
                        List<ImportSupplierItem> SuppItem = new List<ImportSupplierItem>();
                        for (int row = 2; row <= range.Rows.Count; row++)
                        {


                            ImportSupplierItem p = new ImportSupplierItem();

                            p.SupName = ((Excel.Range)range.Cells[row, 1]).Text;
                            p.Description = ((Excel.Range)range.Cells[row, 2]).Text;
                            p.Uom = ((Excel.Range)range.Cells[row, 3]).Text;
                            p.Price = double.Parse(((Excel.Range)range.Cells[row, 4]).Text);
                            SuppItem.Add(p);
                        }


                        List<SupplierItemModel> sm = APISupplier.newimportsuppliers(token, SuppItem, out string error);
                        ViewBag.supplierlist = sm;
                        List<SupplierItemImportViewModel> sivm = new List<SupplierItemImportViewModel>();
                        List<InventoryModel> invm = new List<InventoryModel>();
                        foreach (SupplierItemModel sim in sm)
                        {
                            SupplierItemImportViewModel sivm1 = new SupplierItemImportViewModel();
                            InventoryModel im = new InventoryModel();
                            sivm1.ItemId = sim.ItemId;
                            sivm1.SupId = sim.SupId;
                            sivm1.SupName = sim.SupName;
                            sivm1.Price = sim.Price;
                            sivm1.Uom = sim.Uom;
                            sivm1.CategoryName = sim.CategoryName;
                            sivm1.Description = sim.Description;

                            im = APIInventory.GetInventoryByItemid(sim.ItemId, token, out String error2);

                            sivm.Add(sivm1);
                            invm.Add(im);


                        }
                        workbook.Close();

                        List<int> itemidlist = new List<int>();
                        foreach (SupplierItemImportViewModel spim in sivm)
                        {
                            itemidlist.Add(spim.ItemId);
                        }


                        Session["id"] = itemidlist;


                        ViewBag.check = true;
                        TempData["import"] = invm;

                        return View(invm);
                    }


                    else
                    {


                        Session["noti"] = true;
                        Session["notitype"] = "error";
                        Session["notititle"] = "Import Error";
                        Session["notimessage"] = "File type is incorrect";

                        ViewBag.Error1 = "File type is incorrect";
                        return View("CreateSuppandItem");
                    }
                }
            }

            catch (Exception ex)
            {
                Session["noti"] = true;
                Session["notitype"] = "error";
                Session["notititle"] = "Import Error";
                Session["notimessage"] = "Invalid data in File. Pls import with correct excel file ";

                ViewBag.Error1 = ex.Message;
                return View("CreateSuppandItem");
            }
        }

        [Authorize(Roles = "Clerk, Manager, Supervisor")]
        [HttpPost]
        public ActionResult Importsupplier(HttpPostedFileBase excelfile)
        {


            string token = GetToken();
            UserModel um = GetUser();
            try
            {
                if (excelfile == null || excelfile.ContentLength == 0)
                {

                    Session["noti"] = true;
                    Session["notitype"] = "error";
                    Session["notititle"] = "Import Error";
                    Session["notimessage"] = "File is Empty ";

                    ViewBag.Error = "File is Empty";
                    return View("CreateSuppandItem");
                }

                else
                {

                    if (excelfile.FileName.EndsWith("xls") || excelfile.FileName.EndsWith("xlsx"))
                    {
                        string path = Server.MapPath("~/Content/" + excelfile.FileName);
                        if (System.IO.File.Exists(path))
                            System.IO.File.Delete(path);
                        excelfile.SaveAs(path);
                        // read data from excel file
                        Excel.Application application = new Excel.Application();
                        Excel.Workbook workbook = application.Workbooks.Open(path);
                        Excel.Worksheet worksheet = workbook.ActiveSheet;
                        Excel.Range range = worksheet.UsedRange;
                        List<SupplierModel> Supp = new List<SupplierModel>();
                        for (int row = 2; row <= range.Rows.Count; row++)
                        {

                            SupplierModel p = new SupplierModel();
                            p.SupName = ((Excel.Range)range.Cells[row, 1]).Text;
                            p.SupEmail = ((Excel.Range)range.Cells[row, 2]).Text;
                            p.SupPhone = int.Parse(((Excel.Range)range.Cells[row, 3]).Text);
                            p.ContactName = ((Excel.Range)range.Cells[row, 4]).Text;
                            p.GstRegNo = ((Excel.Range)range.Cells[row, 5]).Text;

                            Supp.Add(p);


                        }
                        Session["noti"] = true;
                        Session["notitype"] = "success";
                        Session["notititle"] = "Import Supplier";
                        Session["notimessage"] = Supp.Count + "Suppliers are successfully created";

                        List<SupplierModel> sm = APISupplier.importsupplier(token, Supp, out string error);
                        workbook.Close();

                        return RedirectToAction("ShowActiveSupplierlist");
                    }
                    else
                    {
                        Session["noti"] = true;
                        Session["notitype"] = "error";
                        Session["notititle"] = "Import Error";
                        Session["notimessage"] = "File type is incorrect";

                        ViewBag.Error = "File type is incorrect";

                        return View("CreateSuppandItem");
                    }
                }
            }

            catch (Exception ex)
            {
                Session["noti"] = true;
                Session["notitype"] = "error";
                Session["notititle"] = "Import Error";
                Session["notimessage"] = "Invalid data in File. Pls import with correct excel file ";

                ViewBag.Error = ex.Message;

                return View("CreateSuppandItem");
            }
        }

        [Authorize(Roles = "Clerk, Manager, Supervisor")]
        [HttpPost]
        public JsonResult CreateSupplierItem(List<SupplierItemImportViewModel> simvm)
        {

            string token = GetToken();
            UserModel um = GetUser();
            bool result = false;
            SupplierModel sm = new SupplierModel();


            try
            {
                foreach (SupplierItemImportViewModel sim in simvm)
                {
                    ItemModel si = new ItemModel();
                    si.Itemid = sim.ItemId;
                    si.Uom = sim.Uom;
                    si.CatName = sim.CategoryName;
                    si.Description = sim.Description;
                    CategoryModel cat = APICategory.GetCategoryByCatName(token, sim.CategoryName, out string error);
                    si.Catid = cat.Catid;


                    APIItem.UpdateItem(token, si, out string itemerror);
                    List<SupplierModel> sm2 = APISupplier.GetSupplierByStatus(ConSupplier.Active.ACTIVE, token, out string error2);
                    result = true;
                }
            }
            catch (Exception ex)
            {
                RedirectToAction("Index", "Error", new { error = ex.Message });
            }

            return Json(result, JsonRequestBehavior.AllowGet);
        }



        [HttpPost]
        public ActionResult EditImportItem(InventoryViewModel viewmodel)
        {

            string token = GetToken();
            InventoryModel invm = new InventoryModel();
            ItemModel it = new ItemModel();
            CategoryModel c = new CategoryModel();

            invm = APIInventory.GetInventoryByInvid(viewmodel.Invid, token, out string error);
            it = APIItem.GetItemByItemID(viewmodel.Itemid, token, out error);
            string name = viewmodel.CategoryName;
            c = APICategory.GetCategoryByCatName(token, name, out error);

            it.Catid = c.Catid;
            invm.Invid = viewmodel.Invid;
            invm.Itemid = viewmodel.Itemid;
            invm.Stock = viewmodel.Stock;
            invm.ReorderLevel = viewmodel.ReorderLevel;
            invm.ReorderQty = viewmodel.ReorderQty;
            it.Description = viewmodel.ItemDescription;
            it.Uom = viewmodel.UOM;

            try
            {
                invm = APIInventory.UpdateInventory(token, invm, out error);
                it = APIItem.UpdateItem(token, it, out error);

                Session["noti"] = true;
                Session["notitype"] = "success";
                Session["notititle"] = "Update Item";
                Session["notimessage"] = it.Description + "is updated successfully";
                List<InventoryModel> im = new List<InventoryModel>();
                InventoryModel im1 = new InventoryModel();
                List<int> ids = (List<int>)Session["id"];

                for (int i = 0; i < ids.Count; i++)
                {
                    im1 = APIInventory.GetInventoryByItemid(ids[i], token, out string error3);
                    im.Add(im1);

                }

                return View("csvsupplier", im);
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "Error", new { error = ex.Message });
            }

        }
        [Authorize(Roles = "Clerk, Manager, Supervisor")]

        public ActionResult EditImportItem(int id = 0)
        {
            string token = GetToken();
            InventoryModel invm = new InventoryModel();
            ItemModel itm = new ItemModel();
            ViewBag.InventoryModel = invm;
            InventoryViewModel viewmodel = new InventoryViewModel();
            List<CategoryModel> cm = new List<CategoryModel>();
            invm = APIInventory.GetInventoryByInvid(id, token, out string error);

            try
            {
                cm = APICategory.GetAllCategories(token, out error);


                ViewBag.InventoryModel = invm;

                viewmodel.CatId = itm.Catid;
                // itm.CatName;
                ViewBag.CategoryName = invm.CategoryName;
                viewmodel.ItemDescription = invm.ItemDescription;
                viewmodel.Stock = invm.Stock;
                viewmodel.ReorderLevel = invm.ReorderLevel;
                viewmodel.ReorderQty = invm.ReorderQty;
                viewmodel.Itemid = invm.Itemid;
                viewmodel.Invid = invm.Invid;
                viewmodel.UOM = invm.UOM;
                List<String> catname = new List<string>();

                ViewBag.cat = cm;

                foreach (CategoryModel c in cm)
                {
                    catname.Add(c.Name);
                }
                ViewBag.catlist = catname;

            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "Error", new
                {
                    error = ex.Message
                });
            }
            return View(viewmodel);
        }

        #endregion

        #region Author: Thet Aung Zaw

        [Authorize(Roles = "Clerk")]
        public ActionResult ApproveCollectionPoint(int id)
        {
            string token = GetToken();


            DepartmentCollectionPointModel dcpm = new DepartmentCollectionPointModel();
            ViewBag.DepartmentCollectionPointModel = dcpm;
            ApproveCollectionPointViewModel viewmodel = new ApproveCollectionPointViewModel();
            List<DepartmentCollectionPointModel> st = new List<DepartmentCollectionPointModel>();

            try
            {
                dcpm = APICollectionPoint.GetDepartmentCollectionPointByDcpid(token, id, out string error);

                if (dcpm.Status != ConDepartmentCollectionPoint.Status.PENDING)
                {
                    Session["noti"] = true;
                    Session["notitype"] = "error";
                    Session["notititle"] = "Change Request Expired";
                    Session["notimessage"] = "This collection point request has already been cancelled or approved!";
                    return RedirectToAction("Index");
                }


                st = APICollectionPoint.GetDepartmentCollectionPointByStatus(token, 1, out error);

                DepartmentCollectionPointModel active =
                    st.Where(x => x.DeptID.Equals(GetUser().Deptid))
                    .LastOrDefault();
                CollectionPointModel cpActive =
                    APICollectionPoint.GetCollectionPointBycpid
                    (token, active.CpID, out error);
                CollectionPointModel cpPending =
                    APICollectionPoint.GetCollectionPointBycpid
                    (token, dcpm.CpID, out error);
                ViewBag.OldLat = cpActive.Latitude;
                ViewBag.OldLng = cpActive.Longitude;
                ViewBag.NewLat = cpPending.Latitude;
                ViewBag.NewLng = cpPending.Longitude;
                ViewBag.DepartmentCollectionModel = dcpm;
                ViewBag.DepartmentCollectionModel = st;
                viewmodel.CpID = dcpm.DeptCpID;
                viewmodel.DepName = dcpm.DeptName;
                viewmodel.CpName = dcpm.CpName;
                foreach (DepartmentCollectionPointModel p in st)
                {
                    viewmodel.OldCpName = p.CpName;
                }

            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "Error", new { error = ex.Message });
            }
            return View(viewmodel);
        }

        [Authorize(Roles = "Clerk")]
        [HttpPost]
        public ActionResult ApproveCollectionPoint(ApproveCollectionPointViewModel viewmodel)
        {
            string token = GetToken();
            UserModel um = GetUser();
            DepartmentCollectionPointModel dcpm = new DepartmentCollectionPointModel();


            dcpm = APICollectionPoint.GetDepartmentCollectionPointByDcpid(token, viewmodel.CpID, out string error);

            try
            {

                if (!viewmodel.Approve)
                {
                    dcpm = APICollectionPoint.RejectDepartmentCollectionPoint(token, dcpm, out error);

                }

                else if (viewmodel.Approve)
                {
                    dcpm = APICollectionPoint.ConfirmDepartmentCollectionPoint(token, dcpm, out error);

                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "Error", new { error = ex.Message });
            }

        }

        [Authorize(Roles = "Clerk")]
        public ActionResult PendingCollectionPoint()
        {
            string token = GetToken();
            DepartmentCollectionPointModel dcpm = new DepartmentCollectionPointModel();
            ViewBag.DepartmentCollectionPointModel = dcpm;
            PendingCollectionPointViewModel viewmodel = new PendingCollectionPointViewModel();
            List<DepartmentCollectionPointModel> st = new List<DepartmentCollectionPointModel>();

            try
            {
                if (dcpm.Status != ConDepartmentCollectionPoint.Status.PENDING)
                {
                    Session["noti"] = true;
                    Session["notitype"] = "error";
                    Session["notititle"] = "Change Request Expired";
                    Session["notimessage"] = "This collection point request has already been cancelled or approved!";
                    return RedirectToAction("Index");
                }
                st = APICollectionPoint.GetDepartmentCollectionPointByStatus(token, 0, out string error);
                viewmodel.pCP = new List<PendingCP>();
                foreach (DepartmentCollectionPointModel s in st)
                {
                    PendingCP p = new PendingCP();
                    {
                        p.DepName = s.DeptName;
                        p.CpID = s.CpID;
                        p.CpName = s.CpName;
                        p.DepID = s.DeptID;
                        p.DepCpID = s.DeptCpID;
                    };
                    viewmodel.pCP.Add(p);
                }
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "Error", new { error = ex.Message });
            }
            return View(viewmodel);
        }




        [Authorize(Roles = "Clerk, Manager, Supervisor")]
        public ActionResult Manage()
        {
            string token = GetToken();
            List<InventoryModel> invm = new List<InventoryModel>();
            try
            {

                invm = APIInventory.GetAllInventories(token, out string error);
                if (error != "")
                {
                    return RedirectToAction("Index", "Error", new { error });
                }
            }
            catch (Exception ex)
            {
                RedirectToAction("Index", "Error", new { error = ex.Message });
            }

            return View(invm);
        }

        [Authorize(Roles = "Clerk, Manager, Supervisor")]
        public ActionResult EditItem(int id = 0)
        {
            string token = GetToken();
            InventoryModel invm = new InventoryModel();
            ItemModel itm = new ItemModel();
            ViewBag.InventoryModel = invm;
            InventoryViewModel viewmodel = new InventoryViewModel();
            List<CategoryModel> cm = new List<CategoryModel>();
            invm = APIInventory.GetInventoryByInvid(id, token, out string error);

            try
            {
                cm = APICategory.GetAllCategories(token, out error);


                ViewBag.InventoryModel = invm;
                viewmodel.CatId = itm.Catid;
                ViewBag.CategoryName = invm.CategoryName;
                viewmodel.ItemDescription = invm.ItemDescription;
                viewmodel.Stock = invm.Stock;
                viewmodel.ReorderLevel = invm.ReorderLevel;
                viewmodel.ReorderQty = invm.ReorderQty;
                viewmodel.Itemid = invm.Itemid;
                viewmodel.Invid = invm.Invid;
                viewmodel.UOM = invm.UOM;
                List<CodeValue> catlists = new List<CodeValue>();

                ViewBag.cat = cm;

                foreach (CategoryModel c in cm)
                {
                    CodeValue cv = new CodeValue();
                    cv.Code = c.Catid;
                    cv.Value = c.Name;
                    catlists.Add(cv);
                }
                ViewBag.catlist = catlists;

            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "Error", new
                {
                    error = ex.Message
                });
            }
            return View(viewmodel);
        }

        [Authorize(Roles = "Clerk, Manager, Supervisor")]
        [HttpPost]
        public ActionResult EditItem(InventoryViewModel viewmodel)
        {

            string token = GetToken();
            InventoryModel invm = new InventoryModel();
            ItemModel it = new ItemModel();
            CategoryModel c = new CategoryModel();


            try
            {
                invm = APIInventory.GetInventoryByInvid(viewmodel.Invid, token, out string error);
                it = APIItem.GetItemByItemID(viewmodel.Itemid, token, out error);
                string name = viewmodel.CategoryName;
                c = APICategory.GetCategoryByCatName(token, name, out error);

                it.Catid = c.Catid;
                invm.Invid = viewmodel.Invid;
                invm.Itemid = viewmodel.Itemid;
                invm.Stock = viewmodel.Stock;
                invm.ReorderLevel = viewmodel.ReorderLevel;
                invm.ReorderQty = viewmodel.ReorderQty;
                it.Description = viewmodel.ItemDescription;
                it.Uom = viewmodel.UOM;
                invm = APIInventory.UpdateInventory(token, invm, out error);
                it = APIItem.UpdateItem(token, it, out error);

                Session["noti"] = true;
                Session["notitype"] = "success";
                Session["notititle"] = "Update Item";
                Session["notimessage"] = it.Description + "is updated successfully";

                return RedirectToAction("Manage");
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "Error", new { error = ex.Message });
            }

        }
        [Authorize(Roles = "Clerk, Manager, Supervisor")]

        [Authorize(Roles = "Clerk")]
        [HttpPost]
        public ActionResult PendingCollectionPoint(ApproveCollectionPointViewModel viewmodel)
        {
            string token = GetToken();
            DepartmentCollectionPointModel dcpm = new DepartmentCollectionPointModel();
            List<DepartmentCollectionPointModel> dcpms = new List<DepartmentCollectionPointModel>();


            try
            {
                dcpms = APICollectionPoint.GetDepartmentCollectionPointByStatus(token, ConDepartmentCollectionPoint.Status.PENDING, out string error);
                if (dcpms.Count > 0)
                {
                    dcpm = dcpms.Where(x => x.DeptID == viewmodel.DepID && x.CpID == viewmodel.CpID).FirstOrDefault();
                }

                if (!viewmodel.Approve)
                {
                    dcpm = APICollectionPoint.RejectDepartmentCollectionPoint(token, dcpm, out error);

                }

                else if (viewmodel.Approve)
                {
                    dcpm = APICollectionPoint.ConfirmDepartmentCollectionPoint(token, dcpm, out error);

                }

                return RedirectToAction("PendingCollectionPoint");
            }
            catch (Exception ex)
            {
                return RedirectToAction("PendingCollectionPoint", "Error", new { error = ex.Message });
            }

        }


        [Authorize(Roles = "Clerk, Manager, Supervisor")]

        public ActionResult SearchByTransDate(DateTime? startdate, DateTime? enddate)

        {
            string token = GetToken();
            InventoryTransactionViewModel viewmodel = new InventoryTransactionViewModel();
            List<InventoryTransactionModel> intlm = new List<InventoryTransactionModel>();
            ItemModel item = new ItemModel();
            try
            {
                if (startdate == null)
                    startdate = new DateTime(1900, 01, 01);
                if (enddate == null)
                    enddate = new DateTime(2900, 01, 01);
                intlm = APIInventoryTranscation.GetInventoryTransactionByTransDate((DateTime)startdate, (DateTime)enddate, token, out string error);
                viewmodel.InvTrans = new List<InventoryTransactionResultViewModel>();

                foreach (InventoryTransactionModel i in intlm)
                {

                    item = APIItem.GetItemByItemID(i.ItemID, token, out error);
                    var result = new InventoryTransactionResultViewModel();
                    result.ItemID = i.ItemID;
                    result.Description = i.Description;
                    result.UOM = i.UOM;
                    result.Qty = i.Qty;
                    result.Date = i.TransDate;
                    viewmodel.InvTrans.Add(result);
                }
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "Error", new { error = ex.Message });
            }
            return View(viewmodel);
        }

        [Authorize(Roles = "Clerk, Manager, Supervisor")]

        public ActionResult ItemTran(DateTime? startdate, DateTime? enddate, int id = 0)
        {
            string token = GetToken();
            string error = "";
            InventoryTransactionModel invm = new InventoryTransactionModel();
            ViewBag.InventoryModel = invm;
            InventoryTransactionViewModel viewmodel = new InventoryTransactionViewModel();

            List<InventoryTransactionModel> intlm = new List<InventoryTransactionModel>();

            try
            {
                if (startdate == null || enddate == null)
                {
                    intlm = APIInventoryTranscation.GetInventoryTransactionByItemID(id, token, out error);
                }
                else
                {
                    intlm = APIInventoryTranscation.GetInventoryTransactionByTransDate((DateTime)startdate, (DateTime)enddate, token, out error);
                    intlm = intlm.Where(p => p.ItemID == id).ToList();
                }
                viewmodel.InvTrans = new List<InventoryTransactionResultViewModel>();

                List<SupplierModel> sups = APISupplier.GetSupplierByItemId(id, token, out error);
                if (sups.Count > 0)
                {
                    ViewBag.Suppliers = sups.Where(x => x.Active == ConSupplier.Active.ACTIVE).ToList();
                }

                foreach (InventoryTransactionModel i in intlm)
                {

                    var result = new InventoryTransactionResultViewModel();
                    result.ItemID = i.ItemID;
                    result.Description = i.Description;
                    result.UOM = i.UOM;
                    result.Qty = i.Qty;
                    result.Date = i.TransDate;
                    result.Remark = i.Remark;
                    result.Transtype = i.TransType;
                    viewmodel.InvTrans.Add(result);
                }
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "Error", new { error = ex.Message });
            }
            return View(viewmodel);
        }
        #endregion

        #region Author: Hsu Yee Phyo

        [Authorize(Roles = "Clerk, Manager, Supervisor")]
        public ActionResult Inventory()
        {
            string token = GetToken();
            UserModel user = GetUser();

            List<InventoryDetailModel> invtdetail = new List<InventoryDetailModel>();
            List<AdjustmentModel> adj = new List<AdjustmentModel>();
            List<AdjustmentDetailModel> adjdetail = new List<AdjustmentDetailModel>();

            try
            {
                invtdetail = APIInventory.GetAllInventoryDetails(token, out string error);

                adj = APIAdjustment.GetAdjustmentByStatus(token, ConAdjustment.Active.PENDING, out error);
                if (adj != null)
                {
                    foreach (AdjustmentModel ad in adj)
                    {
                        foreach (AdjustmentDetailModel add in ad.Adjds)
                        {
                            //To display Inventory stock & Counted stock  
                            add.IssueDate = (DateTime)ad.Issueddate;
                            add.Stock = invtdetail.Where(x => x.Itemid == add.Itemid).Select(x => x.Stock).FirstOrDefault();
                            add.Adjustedqty += (int)add.Stock;
                            adjdetail.Add(add);
                        }
                    }
                    ViewBag.AdjustmentDetailModel = adjdetail;
                }
                else
                {
                    ViewBag.AdjustmentDetailModel = new List<AdjustmentDetailModel>();
                }

                TempData["inventories"] = invtdetail;

                return View(invtdetail);
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "Error", new
                {
                    error = ex.Message
                });
            }

        }


        [Authorize(Roles = "Clerk, Manager, Supervisor")]
        [HttpPost]
        public JsonResult Inventory(int[] Invid)
        {
            string token = GetToken();
            bool ResultSuccess = true;
            List<InventoryDetailModel> selected = new List<InventoryDetailModel>();
            
            if (Invid.Length < 1)
            {
                RedirectToAction("Inventory");
            }

            else
            {
                List<InventoryDetailModel> ivdm = TempData["inventories"] as List<InventoryDetailModel>;
                foreach (int i in Invid)
                {
                    foreach (InventoryDetailModel ivm in ivdm)
                    {
                        if (i == ivm.Invid)
                        {
                            selected.Add(ivm);
                        }
                    }
                }
            }
            TempData["discrepancy"] = selected;
            return Json(ResultSuccess, JsonRequestBehavior.AllowGet);
        }


        [Authorize(Roles = "Clerk")]
        public ActionResult Adjustment()
        {
            List<InventoryDetailModel> dis = TempData["discrepancy"] as List<InventoryDetailModel>;
            InventoryCheckViewModel ivcvm = new InventoryCheckViewModel();
            try
            {
                ivcvm.Invs = dis;
                ivcvm.InvIDs = new List<int>();
            }
            catch (Exception e)
            {
                return RedirectToAction("Index", "Error", new { error = e.Message });
            }

            return View(ivcvm);
        }

        [Authorize(Roles = "Clerk")]
        [HttpPost]
        public ActionResult Adjustment(List<int> InvID, List<int> Current, List<string> Reason)
        {
            string token = GetToken();
            UserModel user = GetUser();
            List<InventoryDetailModel> invent = APIInventory.GetAllInventoryDetails(token, out string error);
            AdjustmentModel adjust = new AdjustmentModel();
            try
            {
                for (int i = 0; i < InvID.Count; i++)
                {

                    InventoryDetailModel inv = new InventoryDetailModel();
                    inv = invent.Where(x => x.Invid == InvID[i]).FirstOrDefault();
                    inv.Current = Current[i];

                    if ((inv.Current - (int)inv.Stock) == 0)
                    {
                        Session["noti"] = true;
                        Session["notitype"] = "error";
                        Session["notititle"] = "Adjustment Same Qty";
                        Session["notimessage"] = "There is no discrepency as the current qty and stock are the same!";
                        return RedirectToAction("Inventory");
                    }
                    else
                    {
                        AdjustmentDetailModel adjd = new AdjustmentDetailModel(inv.Itemid, (inv.Current - (int)inv.Stock), Reason[i]);
                        adjust.Adjds.Add(adjd);
                    }
                }
                adjust.Issueddate = DateTime.Now;
                adjust.Raisedby = user.Userid;

                adjust = APIAdjustment.CreateAdjustment(token, adjust, out error);
            }
            catch (Exception e)
            {
                return RedirectToAction("Index", "Error", new { error = e.Message });
            }
            Session["noti"] = true;
            Session["notitype"] = "success";
            Session["notititle"] = "Adjustment Form";
            Session["notimessage"] = "Adjustment Form was successfully rasised";
            return RedirectToAction("Inventory");
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