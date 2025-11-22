using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;

namespace Do_an_NoSQL.Models
{
    public class Claim : MongoEntity
    {
        [BsonElement("claim_no")]
        public string ClaimNo { get; set; }

        [BsonElement("policy_no")]
        public string PolicyNo { get; set; }

        [BsonElement("app_no")]
        public string AppNo { get; set; }

        [BsonElement("customer_id")]
        public string CustomerId { get; set; }

        [BsonElement("beneficiary_id")]
        public ObjectId BeneficiaryId { get; set; } // Để ánh xạ đúng ObjectId

        [BsonElement("beneficiary_name")]
        public string BeneficiaryName { get; set; }

        [BsonElement("claim_type")]
        public string ClaimType { get; set; }

        [BsonElement("event_info")]
        public ClaimEventInfo EventInfo { get; set; }

        [BsonElement("status")]
        public string Status { get; set; }

        [BsonElement("payout")]
        public PayoutInfo Payout { get; set; }

        [BsonElement("submitted_at")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime SubmittedAt { get; set; }

        [BsonElement("documents")]
        public List<string> Documents { get; set; }
        [BsonElement("notes")]
        public string Notes { get; set; }

        [BsonElement("created_at")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? CreatedAt { get; set; }

        [BsonElement("received_channel")]
        public string ReceivedChannel { get; set; } // Thêm trường received_channel

        [BsonElement("contract_info")]
        public ContractInfo ContractInfo { get; set; } // Thêm trường contract_info
    }

    // EMBEDDED CLASS - KHÔNG KẾ THỪA MongoEntity
    public class ClaimEventInfo
    {
        [BsonElement("event_date")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime EventDate { get; set; }

        [BsonElement("event_place")]
        public string EventPlace { get; set; }

        [BsonElement("description")]
        public string Description { get; set; }

        [BsonElement("cause")]
        public string Cause { get; set; }

        [BsonElement("related_to_exclusion")]
        public bool RelatedToExclusion { get; set; }
    }

    // EMBEDDED CLASS - KHÔNG KẾ THỪA MongoEntity
    public class PayoutInfo
    {
        [BsonElement("requested_amount")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal RequestedAmount { get; set; }

        [BsonElement("approved_amount")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ApprovedAmount { get; set; }

        [BsonElement("paid_amount")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal PaidAmount { get; set; }

        [BsonElement("pay_method")]
        public string PayMethod { get; set; }

        [BsonElement("paid_at")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? PaidAt { get; set; }

        [BsonElement("reference")]
        public string Reference { get; set; }
    }

    // EMBEDDED CLASS - Mô tả thông tin hợp đồng
    public class ContractInfo
    {
        [BsonElement("policy_no")]
        public string PolicyNo { get; set; }

        [BsonElement("policyholder_name")]
        public string PolicyholderName { get; set; }

        [BsonElement("claimant_name")]
        public string ClaimantName { get; set; }

        [BsonElement("claimant_relation")]
        public string ClaimantRelation { get; set; }

        [BsonElement("contact_address")]
        public string ContactAddress { get; set; }

        [BsonElement("phone")]
        public string Phone { get; set; }

        [BsonElement("email")]
        public string Email { get; set; }

    }
}
