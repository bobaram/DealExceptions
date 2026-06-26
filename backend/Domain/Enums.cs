namespace DealExceptions.Domain;

public enum Priority { Low = 0, Medium = 1, High = 2, Critical = 3 }

public enum ExceptionStatus { New, Pending, InReview, Approved, Rejected, Closed }
