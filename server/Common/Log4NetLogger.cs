﻿using System;
using log4net;
using log4net.Core;

namespace Common
{
    public class Log4NetLogger : LoggerBase
    {
        /// <summary>
        /// The logger used by this instance.
        /// </summary>
        private readonly ILog log4NetLogger;

        /// <summary>
        /// Initializes a new instance of the <see cref="Log4NetLogger"/> class.
        /// </summary>
        /// <param name="type">The type to create a logger for.</param>
        public Log4NetLogger(Type type) : base(type)
        {
            this.log4NetLogger = LogManager.GetLogger(type);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Log4NetLogger"/> class.
        /// </summary>
        /// <param name="name">A custom name to use for the logger.  If null, the type's FullName will be used.</param>
        public Log4NetLogger(string name) : base(name)
        {
            this.log4NetLogger = LogManager.GetLogger(name);
        }

        /// <summary>
        /// Gets the name of the logger.
        /// </summary>
        /// <value>The name of the logger.</value>
        public override string Name
        {
            get
            {
                return this.log4NetLogger.Logger.Name;
            }
        }

        /// <summary>
        /// Gets a value indicating whether messages with Debug severity should be logged.
        /// </summary>
        public override bool IsDebugEnabled
        {
            get { return this.log4NetLogger.IsDebugEnabled; }
        }

        /// <summary>
        /// Gets a value indicating whether messages with Info severity should be logged.
        /// </summary>
        public override bool IsInfoEnabled
        {
            get { return this.log4NetLogger.IsInfoEnabled; }
        }

        /// <summary>
        /// Gets a value indicating whether messages with Trace severity should be logged.
        /// </summary>
        public override bool IsTraceEnabled
        {
            get
            {
                return this.log4NetLogger.Logger.IsEnabledFor(Level.Trace);
            }
        }

        /// <summary>
        /// Gets a value indicating whether messages with Warn severity should be logged.
        /// </summary>
        public override bool IsWarnEnabled
        {
            get { return this.log4NetLogger.IsWarnEnabled; }
        }

        /// <summary>
        /// Gets a value indicating whether messages with Error severity should be logged.
        /// </summary>
        public override bool IsErrorEnabled
        {
            get { return this.log4NetLogger.IsErrorEnabled; }
        }

        /// <summary>
        /// Gets a value indicating whether messages with Fatal severity should be logged.
        /// </summary>
        public override bool IsFatalEnabled
        {
            get { return this.log4NetLogger.IsFatalEnabled; }
        }

        /// <summary>
        /// Logs the specified message with Debug severity.
        /// </summary>
        /// <param name="message">The message.</param>
        public override void Debug(string message)
        {
            this.log4NetLogger.Debug(message);
        }

        /// <summary>
        /// Logs the specified message with Debug severity.
        /// </summary>
        /// <param name="format">The message or format template.</param>
        /// <param name="args">Any arguments required for the format template.</param>
        public override void Debug(string format, params object[] args)
        {
            this.log4NetLogger.DebugFormat(format, args);
        }

        /// <summary>
        /// Logs the specified exception with Debug severity.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        /// <param name="format">The message or format template.</param>
        /// <param name="args">Any arguments required for the format template.</param>
        public override void Debug(Exception exception, string format, params object[] args)
        {
            this.log4NetLogger.Debug(args.Length > 0 ? String.Format(format, args) : format, exception);
        }

        /// <summary>
        /// Logs the specified exception with Debug severity.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception to log.</param>
        public override void DebugException(string message, Exception exception)
        {
            this.log4NetLogger.Debug(message, exception);
        }

        /// <summary>
        /// Logs the specified message with Info severity.
        /// </summary>
        /// <param name="message">The message.</param>
        public override void Info(string message)
        {
            this.log4NetLogger.Info(message);
        }

        /// <summary>
        /// Logs the specified message with Info severity.
        /// </summary>
        /// <param name="format">The message or format template.</param>
        /// <param name="args">Any arguments required for the format template.</param>
        public override void Info(string format, params object[] args)
        {
            this.log4NetLogger.InfoFormat(format, args);
        }

        /// <summary>
        /// Logs the specified exception with Info severity.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        /// <param name="format">The message or format template.</param>
        /// <param name="args">Any arguments required for the format template.</param>
        public override void Info(Exception exception, string format, params object[] args)
        {
            this.log4NetLogger.Info(args.Length > 0 ? String.Format(format, args) : format, exception);
        }

        /// <summary>
        /// Logs the specified exception with Info severity.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception to log.</param>
        public override void InfoException(string message, Exception exception)
        {
            this.log4NetLogger.Info(message, exception);
        }

        /// <summary>
        /// Logs the specified message with Trace severity.
        /// </summary>
        /// <param name="message">The message.</param>
        public override void Trace(string message)
        {
            this.log4NetLogger.Logger.Log(Type, Level.Trace, message, null);
        }

        /// <summary>
        /// Logs the specified message with Trace severity.
        /// </summary>
        /// <param name="format">The message or format template.</param>
        /// <param name="args">Any arguments required for the format template.</param>
        public override void Trace(string format, params object[] args)
        {
            this.log4NetLogger.Logger.Log(Type, Level.Trace, args.Length > 0 ? String.Format(format, args) : format, null);
        }

        /// <summary>
        /// Logs the specified exception with Trace severity.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        /// <param name="format">The message or format template.</param>
        /// <param name="args">Any arguments required for the format template.</param>
        public override void Trace(Exception exception, string format, params object[] args)
        {
            this.log4NetLogger.Logger.Log(Type, Level.Trace, args.Length > 0 ? String.Format(format, args) : format, exception);
        }

        /// <summary>
        /// Logs the specified exception with Trace severity.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception to log.</param>
        public override void TraceException(string message, Exception exception)
        {
            this.log4NetLogger.Logger.Log(Type, Level.Trace, message, exception);
        }

        /// <summary>
        /// Logs the specified message with Warn severity.
        /// </summary>
        /// <param name="message">The message.</param>
        public override void Warn(string message)
        {
            this.log4NetLogger.Warn(message);
        }

        /// <summary>
        /// Logs the specified message with Warn severity.
        /// </summary>
        /// <param name="format">The message or format template.</param>
        /// <param name="args">Any arguments required for the format template.</param>
        public override void Warn(string format, params object[] args)
        {
            this.log4NetLogger.WarnFormat(format, args);
        }

        /// <summary>
        /// Logs the specified exception with Warn severity.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        /// <param name="format">The message or format template.</param>
        /// <param name="args">Any arguments required for the format template.</param>
        public override void Warn(Exception exception, string format, params object[] args)
        {
            this.log4NetLogger.Warn(args.Length > 0 ? String.Format(format, args) : format, exception);
        }

        /// <summary>
        /// Logs the specified message with Warn severity.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception to log.</param>
        public override void WarnException(string message, Exception exception)
        {
            this.log4NetLogger.Warn(message, exception);
        }

        /// <summary>
        /// Logs the specified message with Error severity.
        /// </summary>
        /// <param name="message">The message.</param>
        public override void Error(string message)
        {
            this.log4NetLogger.Error(message);
        }

        /// <summary>
        /// Logs the specified message with Error severity.
        /// </summary>
        /// <param name="format">The message or format template.</param>
        /// <param name="args">Any arguments required for the format template.</param>
        public override void Error(string format, params object[] args)
        {
            this.log4NetLogger.ErrorFormat(format, args);
        }

        /// <summary>
        /// Logs the specified exception with Error severity.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        /// <param name="format">The message or format template.</param>
        /// <param name="args">Any arguments required for the format template.</param>
        public override void Error(Exception exception, string format, params object[] args)
        {
            this.log4NetLogger.Error(args.Length > 0 ? String.Format(format, args) : format, exception);
        }

        /// <summary>
        /// Logs the specified exception with Error severity.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception to log.</param>
        public override void ErrorException(string message, Exception exception)
        {
            this.log4NetLogger.Error(message, exception);
        }

        /// <summary>
        /// Logs the specified message with Fatal severity.
        /// </summary>
        /// <param name="message">The message.</param>
        public override void Fatal(string message)
        {
            this.log4NetLogger.Fatal(message);
        }

        /// <summary>
        /// Logs the specified message with Fatal severity.
        /// </summary>
        /// <param name="format">The message or format template.</param>
        /// <param name="args">Any arguments required for the format template.</param>
        public override void Fatal(string format, params object[] args)
        {
            this.log4NetLogger.FatalFormat(format, args);
        }

        /// <summary>
        /// Logs the specified exception with Fatal severity.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        /// <param name="format">The message or format template.</param>
        /// <param name="args">Any arguments required for the format template.</param>
        public override void Fatal(Exception exception, string format, params object[] args)
        {
            this.log4NetLogger.Fatal(args.Length > 0 ? String.Format(format, args) : format, exception);
        }

        /// <summary>
        /// Logs the specified exception with Fatal severity.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception to log.</param>
        public override void FatalException(string message, Exception exception)
        {
            this.log4NetLogger.Fatal(message, exception);
        }
    }
}