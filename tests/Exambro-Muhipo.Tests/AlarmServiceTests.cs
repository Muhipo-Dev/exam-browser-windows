using System;
using ExambroMuhipo.Services;
using Xunit;

namespace ExambroMuhipo.Tests;

public class AlarmServiceTests
{
    [Fact]
    public void AlarmService_ShouldInitializeWithoutErrors()
    {
        var alarm = new AlarmService();
        Assert.NotNull(alarm);
    }

    [Fact]
    public void AlarmService_PlayAndStop_ShouldExecuteCleanly()
    {
        var alarm = new AlarmService();
        
        // Memastikan eksekusi sirine dan penghentian tidak melempar pengecualian
        var exPlay = Record.Exception(() => alarm.PlayExitSiren());
        Assert.Null(exPlay);

        var exStop = Record.Exception(() => alarm.StopAlarm());
        Assert.Null(exStop);
    }

    [Fact]
    public void AlarmService_PlaySecurityBuzzer_ShouldExecuteCleanly()
    {
        var alarm = new AlarmService();

        var ex = Record.Exception(() => alarm.PlaySecurityBuzzer());
        Assert.Null(ex);

        alarm.StopAlarm();
    }
}
