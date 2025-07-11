﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace MTCS.Data.Models;

public partial class Driver
{
    public string DriverId { get; set; }

    public string FullName { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string Email { get; set; }

    public string Password { get; set; }

    public string PhoneNumber { get; set; }

    public int? Status { get; set; }

    public DateTime? CreatedDate { get; set; }

    public string CreatedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public string ModifiedBy { get; set; }

    public DateTime? DeletedDate { get; set; }

    public string DeletedBy { get; set; }

    public int? TotalProcessedOrders { get; set; }

    public virtual ICollection<DriverDailyWorkingTime> DriverDailyWorkingTimes { get; set; } = new List<DriverDailyWorkingTime>();

    public virtual ICollection<DriverFile> DriverFiles { get; set; } = new List<DriverFile>();

    public virtual ICollection<DriverWeeklySummary> DriverWeeklySummaries { get; set; } = new List<DriverWeeklySummary>();

    public virtual ICollection<Trip> Trips { get; set; } = new List<Trip>();
}