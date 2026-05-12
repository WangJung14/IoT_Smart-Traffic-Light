namespace SmartTrafficLight_Domain.Entities;

/// <summary>
/// Ghi lại mỗi lần Camera AI phân tích giao thông và Webster tính toán thời gian đèn.
/// Lưu trữ để tra cứu lịch sử và phân tích xu hướng giao thông.
/// </summary>
public class DetectionLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Thời điểm thực hiện phân tích</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // ── Số lượng xe hướng Bắc–Nam ──────────────────────────
    public int NsCars       { get; set; }
    public int NsMotorbikes { get; set; }
    public int NsBuses      { get; set; }
    public int NsTrucks     { get; set; }

    // ── Số lượng xe hướng Đông–Tây ─────────────────────────
    public int EwCars       { get; set; }
    public int EwMotorbikes { get; set; }
    public int EwBuses      { get; set; }
    public int EwTrucks     { get; set; }

    // ── Kết quả Webster ────────────────────────────────────
    public int    CalculatedCycleTime { get; set; }
    public int    CalculatedGreenNS   { get; set; }
    public int    CalculatedGreenEW   { get; set; }
    public double TotalFlowRatio      { get; set; }
    public string Status              { get; set; } = string.Empty;

    /// <summary>Nguồn dữ liệu: "IMAGE" hoặc "VIDEO"</summary>
    public string Source { get; set; } = "VIDEO";
}
