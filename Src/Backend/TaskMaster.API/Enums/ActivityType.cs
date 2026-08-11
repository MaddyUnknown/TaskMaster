namespace TaskMaster.API.Enums
{
    public enum ActivityType
    {
        JobCreated = 0,
        JobAssigned = 1,
        JobCompleted = 2,
        JobFailed = 3,
        WorkerRegistered = 4,
        WorkerInactive = 5,
        WorkerRemoved = 6
    }
}
