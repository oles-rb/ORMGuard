using System.Diagnostics;

namespace ORM.Trace.Runtime
{

    /// <summary>
    /// Groups database calls made by one logical application operation into an independent ORM Trace trace.
    /// </summary>
    public sealed class OrmTraceTrace : IDisposable
    {
        internal const string OperationTag = "orm-trace.operation";
        private readonly Activity activity;
        private readonly Activity? previousActivity;
        private bool disposed;

        private OrmTraceTrace(Activity activity, Activity? previousActivity)
        {
            this.activity = activity;
            this.previousActivity = previousActivity;
        }

        /// <summary>
        /// Starts a new root trace for one user action, background job, or desktop operation.
        /// The trace does not inherit a long-lived Blazor circuit, SignalR connection, or ambient activity.
        /// </summary>
        public static OrmTraceTrace Start(string operationName)
        {
            if (string.IsNullOrWhiteSpace(operationName))
                throw new ArgumentException("An operation name is required.", nameof(operationName));

            var previousActivity = Activity.Current;
            Activity.Current = null;

            try
            {
                var activity = new Activity(operationName.Trim())
                    .SetIdFormat(ActivityIdFormat.W3C);

                activity.SetTag(OperationTag, operationName.Trim());
                activity.Start();
                return new OrmTraceTrace(activity, previousActivity);
            }
            catch
            {
                Activity.Current = previousActivity;
                throw;
            }
        }

        /// <summary>Ends the operation trace and restores the previously active activity.</summary>
        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            activity.Stop();
            Activity.Current = previousActivity;
        }
    }
}
