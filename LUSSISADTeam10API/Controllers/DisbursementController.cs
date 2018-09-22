﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using LUSSISADTeam10API.Models.APIModels;
using LUSSISADTeam10API.Models.DBModels;
using LUSSISADTeam10API.Repositories;
using LUSSISADTeam10API.Constants;

// Author : Aung Myo
namespace LUSSISADTeam10API.Controllers
{
    // to allow access only by login user
    [Authorize]
    public class DisbursementController : ApiController
    {
        // to Get all Disbursements
        [HttpGet]
        [Route("api/disbursement")]
        public IHttpActionResult GetAllDisbursement()
        {

            List<DisbursementModel> reqs = DisbursementRepo.GetAllDisbursement(out string error);
            return Ok(reqs);
        }
        // to Get all Disbursements with Disbursement Details
        [HttpGet]
        [Route("api/disburdetails")]
        public IHttpActionResult GetAllDisbursementwtihDetails()
        {

            List<DisbursementModel> reqs = DisbursementRepo.GetAllDisbursementwithDetails(out string error);
            return Ok(reqs);
        }
        // to Get all Disbursements with disbursement id
        [HttpGet]
        [Route("api/disbursement/disid/{disid}")]
        public IHttpActionResult GetDisbursementByDisid(int disid)
        {
            string error = "";
            DisbursementModel dism = DisbursementRepo.GetDisbursementByDisbursementId(disid, out error);
            if (error != "" || dism == null)
            {
                if (error == ConError.Status.NOTFOUND)
                {
                    return Content(HttpStatusCode.NotFound, "Requisition Not Found");
                }
                return Content(HttpStatusCode.BadRequest, error);
            }
            return Ok(dism);
        }
        // to Get all Disbursements with requisition id 
        [HttpGet]
        [Route("api/disbursement/reqid/{reqid}")]
        public IHttpActionResult GetDisbursementByRequisitionid(int reqid)
        {
            string error = "";
            List<DisbursementModel> dism = DisbursementRepo.GetDisbursementByRequisitionId(reqid, out error);
            if (error != "" || dism == null)
            {
                if (error == ConError.Status.NOTFOUND)
                {
                    return Content(HttpStatusCode.NotFound, "Requisition Not Found");
                }
                return Content(HttpStatusCode.BadRequest, error);
            }
            return Ok(dism);
        }
        // to Get all Disbursements with acknowledge by id
        [HttpGet]
        [Route("api/disbursement/ackby/{ackby}")]
        public IHttpActionResult GetDisbursementByackbyid(int ackby)
        {
            string error = "";
            List<DisbursementModel> dism = DisbursementRepo.GetDisbursementByackyby(ackby, out error);
            if (error != "" || dism == null)
            {
                if (error == ConError.Status.NOTFOUND)
                {
                    return Content(HttpStatusCode.NotFound, "Requisition Not Found");
                }
                return Content(HttpStatusCode.BadRequest, error);
            }
            return Ok(dism);
        }
        // to Get all Disbursements Details
        [HttpGet]
        [Route("api/disbursementdetails")]
        public IHttpActionResult GetAlldisbursementdetails()
        {

            List<DisbursementDetailsModel> reqds = DisbursementDetailsRepo.GetAllDisbursementDetails(out string error);
            return Ok(reqds);
        }
        // to Get all Disbursements Details with Disbursement ID
        [HttpGet]
        [Route("api/disbursementdetails/disid/{disid}")]
        public IHttpActionResult GetDisbursementDetailsBydisid(int disid)
        {
            string error = "";
            List<DisbursementDetailsModel> disdm = DisbursementDetailsRepo.GetDisbursementDetailsByDisbursementId(disid, out error);
            if (error != "" || disdm == null)
            {
                if (error == ConError.Status.NOTFOUND)
                {
                    return Content(HttpStatusCode.NotFound, "Requisition Not Found");
                }
                return Content(HttpStatusCode.BadRequest, error);
            }
            return Ok(disdm);
        }

        // to Get all Disbursements Details with Item ID
        [HttpGet]
        [Route("api/disbursementdetails/itemid/{itemid}")]
        public IHttpActionResult GetDisbursementDetailsByreqid(int itemid)
        {
            string error = "";
            List<DisbursementDetailsModel> disdm = DisbursementDetailsRepo.GetDisbursementDetailsByItemId(itemid, out error);
            if (error != "" || disdm == null)
            {
                if (error == ConError.Status.NOTFOUND)
                {
                    return Content(HttpStatusCode.NotFound, "Requisition Not Found");
                }
                return Content(HttpStatusCode.BadRequest, error);
            }
            return Ok(disdm);
        }
        // to create new disbursement 
        [HttpPost]
        [Route("api/disbursement/create")]
        public IHttpActionResult Createdisbursement(DisbursementModel dism)
        {
            string error = "";
            DisbursementModel disbm = DisbursementRepo.CreateDisbursement(dism, out error);
            if (error != "" || disbm == null)
            {
                return Content(HttpStatusCode.BadRequest, error);
            }
            return Ok(disbm);
        }
        // to create new disbursement  Details
        [HttpPost]
        [Route("api/disbursementdetails/create")]
        public IHttpActionResult CreateDisbursementDetails(DisbursementDetailsModel dism)
        {
            string error = "";
            DisbursementDetailsModel disbm = DisbursementDetailsRepo.CreateDisbursementDetails(dism, out error);

            // get the inventory using the item id from Requisition details
            InventoryModel invm = InventoryRepo.GetInventoryByItemid(dism.Itemid, out error);

            // subtract  the stock accoring to  qty
            invm.Stock -= dism.Qty;

            // update the inventory
            invm = InventoryRepo.UpdateInventory(invm, out error);


            InventoryTransactionModel invtm = new InventoryTransactionModel
            {
                InvID = invm.Invid,
                ItemID = invm.Itemid,
                Qty = dism.Qty * -1,
                TransType = ConInventoryTransaction.TransType.DISBURSEMENT,
                TransDate = DateTime.Now,
                Remark = dism.Disid.ToString()
            };
            invtm = InventoryTransactionRepo.CreateInventoryTransaction(invtm, out error);



            if (error != "" || disbm == null)
            {
                return Content(HttpStatusCode.BadRequest, error);
            }
            return Ok(disbm);
        }
        // to upadate  disbursement 
        [HttpPost]
        [Route("api/disbursement/update")]
        public IHttpActionResult UpadateDisbursement(DisbursementModel dism)
        {
            string error = "";
            DisbursementModel disbm = DisbursementRepo.UpdateDisbursement(dism, out error);
            if (error != "" || disbm == null)
            {
                return Content(HttpStatusCode.BadRequest, error);
            }
            return Ok(disbm);
        }

        // to update disbursement  Details
        [HttpPost]
        [Route("api/disbursementdetails/update")]
        public IHttpActionResult UpadateDisbursementDetails(DisbursementDetailsModel dism)
        {
            string error = "";
            List<DisbursementDetailsModel> disbm = DisbursementDetailsRepo.UpdateDisbursementDetails(dism, out error);
            if (error != "" || disbm == null)
            {
                return Content(HttpStatusCode.BadRequest, error);
            }
            return Ok(disbm);
        }

        // to create new disbursement with details
        [HttpPost]
        [Route("api/disbursement/createdetails")]
        public IHttpActionResult CreateRequisitionwithDetails(DisbursementModel dism)
        {
            string error = "";
            DisbursementModel dis = DisbursementRepo.CreateDisbursementwithDetails(dism, dism.Disbursementlist, out error);
            if (error != "" || dis == null)
            {
                return Content(HttpStatusCode.BadRequest, error);
            }
            return Ok(dis);
        }
        [HttpGet]
        [Route("api/disbursement/clerk")]
        public IHttpActionResult GetRetriveItemListforClerk()
        {
            // declare and initialize error variable to accept the error from Repo
            string error = "";

            // get the list from Repo and convert it into outstanding item list
            List<OutstandingItemModel> items = DisbursementDetailsRepo.GetAllPreparingItems(out error);

            // if the erorr is not blank or the outstanding list is null
            if (error != "" || items == null)
            {
                // if the error is 404
                if (error == ConError.Status.NOTFOUND)
                    return Content(HttpStatusCode.NotFound, "Outstanding list Not Found");
                // if the error is other one
                return Content(HttpStatusCode.BadRequest, error);
            }
            // if there is no error
            return Ok(items);

        }

        [HttpGet]
        [Route("api/disbursement/BreakDown")]
        public IHttpActionResult GetBreakdownByDepartment()
        {
            // declare and initialize error variable to accept the error from Repo
            string error = "";

            // get the list from Repo and convert it into outstanding item list
            List<BreakdownByDepartmentModel> breakdown = DisbursementDetailsRepo.GetBreakdownByDepartment(out error);

            // if the erorr is not blank or the outstanding list is null
            if (error != "" || breakdown == null)
            {
                // if the error is 404
                if (error == ConError.Status.NOTFOUND)
                    return Content(HttpStatusCode.NotFound, "Breakdown list Not Found");
                // if the error is other one
                return Content(HttpStatusCode.BadRequest, error);
            }
            // if there is no error
            return Ok(breakdown);

        }


    }
}
