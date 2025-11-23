namespace Project_Photo.Areas.Admin.ViewModels.SubscriptionPlanDashboard
{
    public class SubscriptionPlanDashboardIndexViewModel
    {
        public class IndexViewModel
        {
            public int TotalPlans { get; set; }
            public int ActivePlans { get; set; }
            public int PublicPlans { get; set; }
            public int TotalSubscriptions { get; set; }

            public List<PlanLevelStat> PlansByLevel { get; set; }
            public List<SystemStat> SystemStats { get; set; }
            public List<RecentPlanItem> RecentPlans { get; set; }
        }

        public class PlanLevelStat
        {
            public int Level { get; set; }
            public int Count { get; set; }
        }

        public class SystemStat
        {
            public string SystemName { get; set; }
            public string SystemCode { get; set; }
            public int Count { get; set; }
            public int ActiveCount { get; set; }
        }

        public class RecentPlanItem
        {
            public int PlanId { get; set; }
            public string PlanCode { get; set; }
            public string PlanName { get; set; }
            public int PlanLevel { get; set; }
            public decimal MonthlyPrice { get; set; }
            public string SystemName { get; set; }
            public bool IsActive { get; set; }
            public bool IsPublic { get; set; }
            public DateTime UpdateAt { get; set; }
        }
    }
}