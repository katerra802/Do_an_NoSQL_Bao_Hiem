using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Do_an_NoSQL.Models
{
    public class PremiumPayment : MongoEntity
    {
        [BsonElement("policy_no")]
        public string PolicyNo { get; set; }

        // THÊM 3 FIELDS MỚI:
        [BsonElement("related_schedule_id")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string RelatedScheduleId { get; set; }  // Link với PaymentSchedule

        [BsonElement("payment_type")]
        public string PaymentType { get; set; }  // "normal", "penalty", "late_fee"

        [BsonElement("penalty_amount")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal PenaltyAmount { get; set; }  // Số tiền phạt
        //

        [BsonElement("due_date")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime DueDate { get; set; }

        [BsonElement("paid_date")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? PaidDate { get; set; }

        [BsonElement("amount")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Amount { get; set; }

        [BsonElement("status")]
        public string Status { get; set; }

        // ✅ Kênh thanh toán (bank, agent, mobile_app, office)
        [BsonElement("channel")]
        public string Channel { get; set; }

        // ✅ Thêm phương thức thanh toán (bank_transfer, cash, online, credit_card)
        [BsonElement("pay_method")]
        public string PayMethod { get; set; }

        [BsonElement("reference")]
        public string Reference { get; set; }

        [BsonElement("created_at")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? CreatedAt { get; set; }
    }
}
