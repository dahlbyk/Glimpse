﻿using System;
using System.Runtime.Remoting.Messaging;
using System.Web;
using Glimpse.AspNet.Extensions;
using Glimpse.Core.Extensibility;
using Glimpse.Core.Framework;

namespace Glimpse.AspNet
{
    public class AspNetFrameworkProvider : IFrameworkProvider
    {
        /// <summary>
        /// Wrapper around HttpContext.Current for testing purposes. Not for public use.
        /// </summary>
        private HttpContextBase context;

        public AspNetFrameworkProvider(ILogger logger)
        {
            Logger = logger;
        }

        public IDataStore HttpRequestStore
        {
            get { return new DictionaryDataStoreAdapter(Context.Items); }
        }

        public IDataStore HttpServerStore
        {
            get { return new HttpApplicationStateBaseDataStoreAdapter(Context.Application); }
        }

        public object RuntimeContext
        {
            get { return Context; }
        }

        public IRequestMetadata RequestMetadata
        {
            get { return new RequestMetadata(Context); }
        }

        internal HttpContextBase Context
        {
            get { return context ?? GetOrCaptureLogicalContext(); }
            set { context = value; }
        }

        private static HttpContextBase GetOrCaptureLogicalContext()
        {
            if (HttpContext.Current == null)
            {
                var wrapper = CallContext.LogicalGetData("Glimpse.HttpContext") as SerializableHttpContextWrapper;
                return wrapper == null ? null : wrapper.Context;
            }
            else
            {
                var wrapper = new SerializableHttpContextWrapper();
                CallContext.LogicalSetData("Glimpse.HttpContext", wrapper);
                return wrapper.Context;
            }
        }

        private ILogger Logger { get; set; }

        public void SetHttpResponseHeader(string name, string value)
        {
            if (!Context.HeadersSent())
            {
                try
                {
                    Context.Response.AppendHeader(name, value);
                }
                catch (Exception exception)
                {
                    Logger.Error("Exception setting Http response header '{0}' with value '{1}'.", exception, name, value);
                }
            }
        }

        public void SetHttpResponseStatusCode(int statusCode)
        {
            try
            {
                Context.Response.StatusCode = statusCode;
                Context.Response.StatusDescription = null;
            }
            catch (Exception exception)
            {
                Logger.Error("Exception setting Http status code with value '{0}'.", exception, statusCode);
            }
        }

        public void SetCookie(string name, string value)
        {
            try
            {
                Context.Response.Cookies.Add(new HttpCookie(name, value));
            }
            catch (Exception exception)
            {
                Logger.Error("Exception setting cookie '{0}' with value '{1}'.", exception, name, value);
            }
        }

        public void InjectHttpResponseBody(string htmlSnippet)
        {
            try
            {
                var response = Context.Response;
                response.Filter = new PreBodyTagFilter(htmlSnippet, response.Filter, response.ContentEncoding, Context.Request != null ? Context.Request.RawUrl : null, Logger);
            }
            catch (Exception exception)
            {
                Logger.Error("Exception injecting Http response body with Html snippet '{0}'.", exception, htmlSnippet);
            }
        }

        public void WriteHttpResponse(byte[] content)
        {
            try
            {
                Context.Response.BinaryWrite(content);
            }
            catch (Exception exception)
            {
                Logger.Error("Exception writing Http response.", exception);
            }
        }

        public void WriteHttpResponse(string content)
        {
            try
            {
                Context.Response.Write(content);
            }
            catch (Exception exception)
            {
                Logger.Error("Exception writing Http response.", exception);
            }
        }
    }

    [Serializable]
    public class SerializableHttpContextWrapper
    {
        [NonSerialized]
        private HttpContextBase current;

        public SerializableHttpContextWrapper()
        {
            if (Context == null)
            {
                throw new InvalidOperationException("Missing HttpContext");
            }
        }

        public HttpContextBase Context
        {
            get { return current ?? (current = new HttpContextWrapper(HttpContext.Current)); }
        }
    }
}