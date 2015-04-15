using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Threading;

public static class Parallel {

	class ParallelUnitOfWork<T>
	{
		public T Subject { get; private set; }
		public Action<T> Body { get; private set; }
		public ManualResetEvent ResetEvent { get; private set; }

		public ParallelUnitOfWork(T subject, Action<T> body, ManualResetEvent resetEvent)
		{
			Subject = subject;
			Body = body;
			ResetEvent = resetEvent;
		}
	}

	/// <summary>
	/// Executes the provided Action<T> once for each item in the provided IEnumerable<T>.
	/// </summary>
	/// <param name="source">Source</param>
	/// <param name="body">Body</param>
	/// <typeparam name="T">The 1st type parameter</typeparam>
	public static void ForEach<T>(IEnumerable<T> source, Action<T> body)
	{
		List<ManualResetEvent> resetEvents = new List<ManualResetEvent>();
		foreach(var item in source)
		{
			var resetEvent = new ManualResetEvent(false);
			resetEvents.Add(resetEvent);
			ThreadPool.QueueUserWorkItem(
				new WaitCallback(ExecuteForEachBody<T>), new ParallelUnitOfWork<T>(item, body, resetEvent));
		}

		WaitHandle.WaitAll(resetEvents.ToArray());
	}

	private static void ExecuteForEachBody<T>(object state)
	{
		var unitOfWork = state as ParallelUnitOfWork<T>;
		if(unitOfWork == null)
			return;

		try {
			unitOfWork.Body(unitOfWork.Subject);
		}catch(Exception e)
		{
			throw new UnityException("Exception during parallel execution: " + e.Message, e);
		}

		unitOfWork.ResetEvent.Set();
	}

	class ParallelUnitOfWork
	{
		public int Iteration { get; private set; }
		public Action<int> Body { get; private set; }
		public ManualResetEvent ResetEvent { get; private set; }

		public ParallelUnitOfWork(int iteration, Action<int> body, ManualResetEvent resetEvent)
		{
			Iteration = iteration;
			Body = body;
			ResetEvent = resetEvent;
		}
	}

	/// <summary>
	/// Executes the provided Action<int> once for each demanded iteration
	/// </summary>
	/// <param name="iterations">Iterations.</param>
	/// <param name="body">Body.</param>
	public static void For(int iterations, Action<int> body)
	{
		ManualResetEvent[] resetEvents = new ManualResetEvent[iterations];
		for(int i = 0; i < iterations; i++)
		{
			resetEvents[i] = new ManualResetEvent(false);
			ThreadPool.QueueUserWorkItem(new WaitCallback(ExecuteForBody), 
			                             new ParallelUnitOfWork(i, body, resetEvents[i]));
		}
		WaitHandle.WaitAll(resetEvents);
	}

	private static void ExecuteForBody(object state)
	{
		var unitOfWork = state as ParallelUnitOfWork;
		if(unitOfWork == null)
			return;

		try {
			unitOfWork.Body(unitOfWork.Iteration);
		}catch(Exception e)
		{
			throw new UnityException("Exception during parallel execution: " + e.Message, e);
		}

		unitOfWork.ResetEvent.Set();
	}

}
