using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using FramePro;

namespace ProfilerStudy.McpServer;

internal static class ProfilerDiagnosticsSelfTest
{
	public static int Run()
	{
		try
		{
			List<ProfilerDiagnostics.FrameSample> samples = new List<ProfilerDiagnostics.FrameSample>
			{
				new ProfilerDiagnostics.FrameSample(10, 20.0, 100, 1000),
				new ProfilerDiagnostics.FrameSample(15, 96.0, 100, 1000),
				new ProfilerDiagnostics.FrameSample(20, 132.0, 100, 1000),
				new ProfilerDiagnostics.FrameSample(25, 133.0, 100, 1000),
				new ProfilerDiagnostics.FrameSample(30, 132.5, 100, 1000)
			};

			Dictionary<string, object> pattern = ProfilerDiagnostics.AnalyzeSlowFramePattern(samples, 50.0);
			AssertEqual(true, pattern["hasPeriodicSlowFrames"], "periodic slow frames");
			AssertEqual(5, pattern["dominantIndexDelta"], "dominant delta");
			RunAnalysisServiceTests();

			return 0;
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine(ex.GetType().FullName);
			Console.Error.WriteLine(ex.Message);
			Console.Error.WriteLine(ex.StackTrace ?? string.Empty);
			return 1;
		}
	}

	private static void RunAnalysisServiceTests()
	{
		ProfilerAnalysisService service = new ProfilerAnalysisService();
		Session session = new Session(new CoreSettings(), new CapturingLog());
		session.FillWithRandomData();
		InvokePrivate(session, "AssignUnassignedTimeSpans");
		PopulateTimerNamesFromStats(session);
		PopulateCounter(session);
		string sessionId = AddSession(service, session);
		Dictionary<string, object> hotspots = service.FindScopeHotspots(sessionId, 10, -1, -1);
		AssertHasItems(hotspots["scopeHotspots"], "scope hotspots");

		Dictionary<string, object> frame = service.AnalyzeFrame(sessionId, 20, 10, 3);
		AssertHasItems(frame["scopeHotspots"], "frame scope hotspots");
		AssertHasItems(frame["neighborSlowFrames"], "neighbor slow frames");

		Dictionary<string, object> frameDetail = service.AnalyzeFrameDetail(sessionId, 20, 20, 4, 0.0);
		AssertHasItems(frameDetail["threadFlameGraphs"], "thread flame graphs");
		AssertHasItems(frameDetail["topSpans"], "top frame detail spans");

		Dictionary<string, object> range = service.AnalyzeTimeRange(sessionId, 18, 24, 10, 0.0);
		AssertHasItems(range["scopeHotspots"], "range scope hotspots");

		Dictionary<string, object> counters = service.ListCounters(sessionId, 10, string.Empty);
		AssertHasItems(counters["counters"], "counters");
		IDictionary firstCounter = ((IList)counters["counters"])[0] as IDictionary;
		string counterName = Convert.ToString(firstCounter["name"]);
		Dictionary<string, object> counterSamples = service.QueryCounterSamples(sessionId, counterName, 18, 24, false, 10);
		AssertEqual(counterName, counterSamples["counterName"], "counter name");
		AssertHasItems(counterSamples["samples"], "counter samples");
		service.CloseSession(sessionId);
	}

	private static string AddSession(ProfilerAnalysisService service, Session session)
	{
		MethodInfo method = typeof(ProfilerAnalysisService).GetMethod("AddSession", BindingFlags.Instance | BindingFlags.NonPublic);
		return (string)method.Invoke(service, new object[] { session, "self-test", new CapturingLog() });
	}

	private static void InvokePrivate(object target, string name)
	{
		MethodInfo method = target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
		method.Invoke(target, new object[0]);
	}

	private static void PopulateTimerNamesFromStats(Session session)
	{
		FieldInfo statsField = typeof(Session).GetField("m_TimeSpanFrameStats", BindingFlags.Instance | BindingFlags.NonPublic);
		FieldInfo namesField = typeof(Session).GetField("m_TimeSpanNames", BindingFlags.Instance | BindingFlags.NonPublic);
		IDictionary stats = (IDictionary)statsField.GetValue(session);
		object names = namesField.GetValue(session);
		MethodInfo add = names.GetType().GetMethod("Add");
		foreach (object key in stats.Keys)
		{
			add.Invoke(names, new object[] { key });
		}
	}

	private static void PopulateCounter(Session session)
	{
		const long counterId = 9001;
		const string counterName = "SelfTestCounter";
		IDictionary strings = (IDictionary)GetPrivateField(session, "m_Strings");
		strings[counterId] = counterName;
		IDictionary stringIds = (IDictionary)GetPrivateField(session, "m_StringIds");
		stringIds[counterName] = counterId;
		IDictionary valueTypes = (IDictionary)GetPrivateField(session, "m_CustomStatValueTypes");
		valueTypes[counterId] = CustomStatValueType.Double;
		IDictionary sessionInfo = (IDictionary)GetPrivateField(session, "m_CustomStatSessionInfo");
		sessionInfo[counterId] = new CustomStatSessionData(counterId, CustomStatValueType.Double, 18)
		{
			m_TotalValueDouble = 196.0,
			m_TotalCount = 7,
			m_MinValuePerFrameDouble = 10.0,
			m_MaxValuePerFrameDouble = 46.0,
			m_MinCountPerFrame = 1,
			m_MaxCountPerFrame = 1,
			m_AccTotalValueDouble = 196.0,
			m_AccMinValuePerFrameDouble = 10.0,
			m_AccMaxValuePerFrameDouble = 196.0
		};
		for (int frameIndex = 18; frameIndex <= 24; frameIndex++)
		{
			Frame frame = session.GetFrame(frameIndex);
			frame.AddCustomStat(counterId, 0, 10.0 + frameIndex - 18, 1);
		}
	}

	private static object GetPrivateField(object target, string name)
	{
		FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
		return field.GetValue(target);
	}

	private static void AssertHasItems(object actual, string name)
	{
		ICollection collection = actual as ICollection;
		if (collection == null || collection.Count == 0)
		{
			throw new InvalidOperationException(name + " expected at least one item.");
		}
	}

	private static void AssertEqual<T>(T expected, object actual, string name)
	{
		if (!object.Equals(expected, actual))
		{
			throw new InvalidOperationException(name + " expected " + expected + " but got " + actual);
		}
	}
}
