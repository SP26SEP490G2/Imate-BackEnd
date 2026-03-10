// Message constants for the application
// Each constant corresponds to a message code used throughout the UI.
// English description is provided as a comment for reference.

namespace Imate.API.Common
{
    public static class Messages
    {
        // MSG01: In red, under text box - Required field/Input validation.
        public const string MSG01 = "Vui lòng điền đầy đủ hoặc chính xác thông tin bắt buộc.";

        // MSG02: Toast message - Authentication error.
        public const string MSG02 = "Tên đăng nhập hoặc mật khẩu không đúng.";

        // MSG03: Toast message - General system error.
        public const string MSG03 = "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.";

        // MSG04: In red, under text box - File upload validation.
        public const string MSG04 = "Tệp không hợp lệ hoặc quá dung lượng.";

        // MSG05: Modal message - System maintenance.
        public const string MSG05 = "Hệ thống đang bảo trì";

        // MSG06: Empty state message - Data unavailability.
        public const string MSG06 = "Hiện tại không có dữ liệu nào sẵn có";

        // MSG07: Toast message - Data fetching error.
        public const string MSG07 = "Không thể tải thông tin. Vui lòng thử lại";

        // MSG08: Toast message - Account status (suspended/locked).
        public const string MSG08 = "Tài khoản hiện tại đang bị khóa.";

        // MSG09: Toast message - Success notification (data update).
        public const string MSG09 = "Cập nhật thành công";

        // MSG10: Toast message - Failure notification (data update).
        public const string MSG10 = "Cập nhật thất bại";

        // MSG11: Toast message - Password reset instructions sent.
        public const string MSG11 = "Vui lòng kiểm tra email để đặt lại mật khẩu";

        // MSG12: Toast message - Question saved successfully.
        public const string MSG12 = "Đã lưu câu hỏi thành công";

        // MSG13: Toast message - Question unsaved.
        public const string MSG13 = "Đã bỏ lưu câu hỏi";

        // MSG14: Toast message - Submission awaiting moderation.
        public const string MSG14 = "Cảm ơn bạn đã đóng góp! Câu hỏi của bạn đang chờ được duyệt";

        // MSG15: Toast message - Item deleted successfully.
        public const string MSG15 = "Xóa thành công";

        // MSG16: Toast message - Item deletion failed.
        public const string MSG16 = "Xóa thất bại";

        // MSG17: Modal message - Confirmation prompt for destructive action.
        public const string MSG17 = "Bạn có chắc chắn muốn xóa không?";

        // MSG18: In red, under text box - Specific file requirement.
        public const string MSG18 = "Tệp phải là PDF hoặc DOCX và dưới 25MB";

        // MSG19: Toast message - System response error (timeout).
        public const string MSG19 = "Không thể nhận phản hồi từ hệ thống. Vui lòng thử lại";

        // MSG20: Toast message - Temporary unavailability (report generation).
        public const string MSG20 = "Mentor đang hoàn tất báo cáo. Vui lòng quay lại sau";

        // MSG21: In red, under text box - Input validation for answer.
        public const string MSG21 = "Câu trả lời của bạn không hợp lệ";

        // MSG22: Toast message - CV uploaded successfully.
        public const string MSG22 = "Tải lên CV thành công";

        // MSG23: Toast message - CV validation/relevance check failed.
        public const string MSG23 = "CV của bạn không hợp lệ hoặc không liên quan đến lĩnh vực IT";

        // MSG24: Toast message - AI/API error.
        public const string MSG24 = "Đã xảy ra lỗi khi tạo phản hồi AI. Vui lòng thử lại sau vài phút.";

        // MSG25: Toast message - Temporary save failure for practice history.
        public const string MSG25 = "Phản hồi tạm thời không thể lưu vào Lịch sử luyện tập của bạn do lỗi hệ thống tạm thời.";

        // MSG26: Modal message - Quota limit / upsell.
        public const string MSG26 = "Bạn đã hết lượt luyện tập. Vui lòng nâng cấp tài khoản hoặc mua thêm lượt";

        // MSG27: Toast message - Configuration saves failure.
        public const string MSG27 = "Không thể lưu cấu hình phỏng vấn. Vui lòng thử lại sau giây lát";

        // MSG28: Toast message - Interview session termination due to error.
        public const string MSG28 = "Đã xảy ra lỗi hệ thống khi tạo câu hỏi tiếp theo. Buổi phỏng vấn đã được kết thúc";

        // MSG29: Toast message - Prerequisite: upload CV before interview setup.
        public const string MSG29 = "Bạn cần tải lên ít nhất một CV hợp lệ trước khi thiết lập phỏng vấn.";

        // MSG30: Toast message - Job Description processing error.
        public const string MSG30 = "Dữ liệu JD không thể xử lý. Vui lòng kiểm tra lại đường dẫn/file hoặc dán nội dung JD đầy đủ.";

        // MSG31: Empty state message - No practice history.
        public const string MSG31 = "Bạn chưa có lịch sử luyện tập nào. Hãy bắt đầu buổi luyện tập đầu tiên của mình!";

        // MSG32: Empty state message - No bookings.
        public const string MSG32 = "Bạn không có ai đặt lịch.";

        // MSG33: Empty state message - No interviews.
        public const string MSG33 = "Bạn chưa có buổi phỏng vấn nào.";

        // MSG34: Empty state message - No reviews.
        public const string MSG34 = "Bạn chưa có đánh giá nào.";

        // MSG35: Empty state - No earnings.
        public const string MSG35 = "Bạn chưa có thu nhập.";

        // MSG36: In red, under text box - Numeric input validation.
        public const string MSG36 = "Giá trị không hợp lệ. Vui lòng nhập một số hợp lệ lớn hơn 0.";

        // MSG37: Toast message - Review submitted successfully.
        public const string MSG37 = "Cảm ơn bạn đã đánh giá";

        // MSG38: In red, under text box - Rating requirement.
        public const string MSG38 = "Vui lòng chọn số sao đánh giá trước khi gửi.";

        // MSG39: Toast message - Submission error for rating.
        public const string MSG39 = "Đã xảy ra lỗi hệ thống khi gửi đánh giá. Vui lòng thử lại sau.";

        // MSG40: Toast message - Payment/Booking failure.
        public const string MSG40 = "Thanh toán thất bại hoặc đã hết thời gian chờ. Buổi đặt lịch chưa được xác nhận. Vui lòng thử lại.";

        // MSG41: Toast message - Resource creation error (interview room).
        public const string MSG41 = "Không thể tạo phòng phỏng vấn, vui lòng thử lại sau.";

        // MSG42: Toast message - Refund initiated.
        public const string MSG42 = "Hoàn tiền cho Candidate đã được khởi tạo, vui lòng xử lý thủ công. Phí đảm bảo được trừ từ tài khoản Mentor.";

        // MSG43: Toast message - Email sending failure.
        public const string MSG43 = "Tạo tài khoản thành công, nhưng chưa gửi được email. Vui lòng thử gửi lại";

        // MSG44: Toast message - Duplicate email.
        public const string MSG44 = "Email này đã được sử dụng cho tài khoản khác";

        // MSG45: Toast message - Unable to load metrics data.
        public const string MSG45 = "Không thể tải dữ liệu số liệu. Vui lòng thử lại sau.";

        // MSG46: Toast message - System malfunction.
        public const string MSG46 = "Lỗi hệ thống, vui lòng thử lại.";

        // MSG47: Toast message - Recruiter application submitted.
        public const string MSG47 = "Đơn đăng ký Nhà tuyển dụng đã được gửi thành công. Vui lòng chờ xét duyệt.";

        // MSG48: Toast message - Job posting created.
        public const string MSG48 = "Đơn tuyển dụng đã được đăng thành công.";

        // MSG49: Toast message - Not authorized to post job listings.
        public const string MSG49 = "Bạn chưa được phép đăng tin tuyển dụng. Cần được phê duyệt.";

        // MSG50: Toast message - Application submitted successfully.
        public const string MSG50 = "Ứng tuyển thành công. Vui lòng chờ nhà tuyển dụng liên hệ với bạn.";

        // MSG51: Toast message - Already applied for this job.
        public const string MSG51 = "Bạn đã ứng tuyển vào vị trí này rồi.";
    }
}
