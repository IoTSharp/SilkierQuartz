using HandlebarsDotNet;
using Newtonsoft.Json;
using SilkierQuartz.Models;
using SilkierQuartz.TypeHandlers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;

using static SilkierQuartz.Controllers.PageControllerBase;

namespace SilkierQuartz.Helpers
{
    internal class HandlebarsHelpers
    {
        Services _services;
        private readonly SilkierQuartzAuthenticationOptions authenticationOptions;
        private readonly string baseUrl;

        public HandlebarsHelpers(Services services, SilkierQuartzAuthenticationOptions authenticationOptions)
        {
            _services = services;
            this.authenticationOptions = authenticationOptions;

            var url = "/";
            url += _services.Options.BasePath.Trim('/');
            if (!url.EndsWith('/'))
            {
                url += '/';
            }
            url += _services.Options.VirtualPathRoot.Trim('/');
            if (!url.EndsWith('/'))
            {
                url += '/';
            }
            baseUrl = url;
        }

        public static void Register(Services services, SilkierQuartzAuthenticationOptions authenticationOptions)
        {
            new HandlebarsHelpers(services, authenticationOptions).RegisterInternal();
        }

        void RegisterInternal()
        {
            var h = _services.Handlebars;

            h.RegisterHelper("Upper", (o, c, a) => o.Write(a[0].ToString().ToUpper()));
            h.RegisterHelper("Lower", (o, c, a) => o.Write(a[0].ToString().ToLower()));
            h.RegisterHelper("LocalTimeZoneInfoId", (o, c, a) => o.Write(TimeZoneInfo.Local.Id));
            h.RegisterHelper("SystemTimeZonesJson", (o, c, a) => Json(o, c, a,TimeZoneInfo.GetSystemTimeZones().ToDictionary()));
            h.RegisterHelper("DefaultDateFormat", (o, c, a) => o.Write(DateTimeSettings.DefaultDateFormat));
            h.RegisterHelper("DefaultTimeFormat", (o, c, a) => o.Write(DateTimeSettings.DefaultTimeFormat));
            h.RegisterHelper("DoLayout", (o, c, a) => (c.Value as Histogram)?.Layout());
            h.RegisterHelper("SerializeTypeHandler", (o, c, a) => o.WriteSafeString(_services.TypeHandlers.Serialize((TypeHandlerBase)c.Value)));
            h.RegisterHelper("Disabled", (o, c, a) => { if (IsTrue(a[0])) o.Write("disabled"); });
            h.RegisterHelper("Checked", (o, c, a) => { if (IsTrue(a[0])) o.Write("checked"); });
            h.RegisterHelper("nvl", (o, c, a) => o.Write(a[a[0] == null ? 1 : 0]));
            h.RegisterHelper("not", (o, c, a) => o.Write(IsTrue(a[0]) ? "False" : "True"));

            h.RegisterHelper(nameof(BaseUrl), (o, c, a) => o.WriteSafeString(BaseUrl));
            h.RegisterHelper(nameof(MenuItemActionLink), MenuItemActionLink);
            h.RegisterHelper(nameof(RenderJobDataMapValue), RenderJobDataMapValue);
            h.RegisterHelper(nameof(ViewBag), ViewBag);
            h.RegisterHelper(nameof(ActionUrl), ActionUrl);
            h.RegisterHelper(nameof(Json), (o, c, a) => Json(o, c, a));
            h.RegisterHelper(nameof(Selected), Selected);
            h.RegisterHelper(nameof(isType), isType);
            h.RegisterHelper(nameof(eachPair), eachPair);
            h.RegisterHelper(nameof(eachItems), eachItems);
            h.RegisterHelper(nameof(ToBase64), ToBase64);
            h.RegisterHelper(nameof(footer), footer);
            h.RegisterHelper(nameof(SilkierQuartzVersion), SilkierQuartzVersion);
            h.RegisterHelper(nameof(Logo), Logo);
            h.RegisterHelper(nameof(ProductName), ProductName);
            h.RegisterHelper(nameof(CustomStyleSheet), CustomStyleSheet);
            h.RegisterHelper(nameof(CustomFavicon), CustomFavicon);
        }

        static bool IsTrue(object value) => value?.ToString()?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;

        string HtmlEncode(object value) => _services.ViewEngine.Encode(value);

        string UrlEncode(string value) => HttpUtility.UrlEncode(value);

        string BaseUrl => this.baseUrl;

        private string AddQueryString(string uri, IEnumerable<KeyValuePair<string, object>> queryString)
        {
            if (queryString == null)
                return uri;

            var anchorIndex = uri.IndexOf('#');
            var uriToBeAppended = uri;
            var anchorText = "";
            // If there is an anchor, then the query string must be inserted before its first occurence.
            if (anchorIndex != -1)
            {
                anchorText = uri.Substring(anchorIndex);
                uriToBeAppended = uri.Substring(0, anchorIndex);
            }

            var queryIndex = uriToBeAppended.IndexOf('?');
            var hasQuery = queryIndex != -1;

            var sb = new StringBuilder();
            sb.Append(uriToBeAppended);

            foreach (var parameter in queryString)
            {
                sb.Append(hasQuery ? '&' : '?');
                sb.Append(UrlEncode(parameter.Key));
                sb.Append('=');
                sb.Append(UrlEncode(string.Format(CultureInfo.InvariantCulture, "{0}", parameter.Value)));
                hasQuery = true;
            }

            sb.Append(anchorText);
            return sb.ToString();
        }

        void ViewBag(EncodedTextWriter output, Context context, Arguments    arguments)
        {
            var dict = (IDictionary<string, object>)arguments[0];
            var viewBag = (IDictionary<string, object>)context["ViewBag"];

            foreach (var pair in dict)
            {
                viewBag[pair.Key] = pair.Value;
            }
        }

        void MenuItemActionLink(EncodedTextWriter output, Context context, Arguments arguments)
        {
            var dict = arguments[0] as IDictionary<string, object> ?? new Dictionary<string, object>() { ["controller"] = arguments[0] };

            if (string.Equals(dict["controller"], "Authenticate/Logout") && (authenticationOptions==null || authenticationOptions.AccessRequirement == SilkierQuartzAuthenticationOptions.SimpleAccessRequirement.AllowAnonymous))
            {
                return;
            }

            var classes = "item";
            if (dict["controller"].Equals(context.GetValue<string>("ControllerName")))
                classes += " active";

            var url = BaseUrl + dict["controller"];
            var title = HtmlEncode(dict.GetValue("title", dict["controller"]));

            output.WriteSafeString($@"<a href=""{url}"" class=""{classes}"">{title}</a>");
        }

        void ActionUrl(EncodedTextWriter output, Context context, Arguments arguments)
        {
            if (arguments.Length < 1 || arguments.Length > 3)
                throw new ArgumentOutOfRangeException(nameof(arguments));

            IDictionary<string, object> routeValues = null;
            string controller = null;
            var action = (arguments[0] as Page)?.ActionName ?? (string)arguments[0];

            if (arguments.Length >= 2) // [actionName, controllerName/routeValues ]
            {
                if (arguments[1] is IDictionary<string, object> r)
                    routeValues = r;
                else if (arguments[1] is string s)
                    controller = s;
                else if (arguments[1] is Page v)
                    controller = v.ControllerName;
                else
                    throw new Exception("ActionUrl: Invalid parameter 1");
            }
            if (arguments.Length == 3) // [actionName, controllerName, routeValues]
                routeValues = (IDictionary<string, object>)arguments[2];

            controller ??= context.GetValue<string>("ControllerName");

            var url = BaseUrl + controller;

            if (!string.IsNullOrEmpty(action))
                url += "/" + action;

            output.WriteSafeString(AddQueryString(url, routeValues));
        }

        void Selected(EncodedTextWriter output, Context context, Arguments arguments)
        {
            string selected;
            if (arguments.Length >= 2)
                selected = arguments[1]?.ToString();
            else
                selected = context["selected"].ToString();

            if (((string)arguments[0]).Equals(selected, StringComparison.InvariantCultureIgnoreCase))
                output.Write("selected");
        }

        void Json(EncodedTextWriter output, Context context, Arguments arguments, params object[] args)
        {
            if (arguments.Length > 0)
            {
                output.WriteSafeString(JsonConvert.SerializeObject(arguments[0]));
            }

            if (args.Length <= 0)
            {
                return;
            }

            output.WriteSafeString(JsonConvert.SerializeObject(args[0]));
        }

        void RenderJobDataMapValue(EncodedTextWriter output, Context context, Arguments arguments)
        {
            var item = (JobDataMapItem)arguments[0];
            output.WriteSafeString(item.SelectedType.RenderView(_services, item.Value));
        }
        void isType(EncodedTextWriter writer, BlockHelperOptions options,   Context context,   Arguments   arguments)
        {
            Type[] expectedType;

            var strType = (string)arguments[1];

            switch (strType)
            {
                case "IEnumerable<string>":
                    expectedType = new[] { typeof(IEnumerable<string>) };
                    break;
                case "IEnumerable<KeyValuePair<string, string>>":
                    expectedType = new[] { typeof(IEnumerable<KeyValuePair<string, string>>) };
                    break;
                default:
                    throw new ArgumentException("Invalid type: " + strType);
            }

            var t = arguments[0]?.GetType();

            if (expectedType.Any(x => x.IsAssignableFrom(t)))
                options.Template(writer,  context.Value);
            else
                options.Inverse(writer,  context.Value);
        }

        void eachPair(EncodedTextWriter writer, BlockHelperOptions options, Context context, Arguments arguments)
        {
            void OutputElements<T>()
            {
                if (arguments[0] is IEnumerable<T> pairs)
                {
                    foreach (var item in pairs)
                        options.Template(writer, item);
                }
            }

            OutputElements<KeyValuePair<string, string>>();
            OutputElements<KeyValuePair<string, object>>();
        }

        void eachItems(EncodedTextWriter writer, BlockHelperOptions options, Context context, Arguments arguments)
        {
            eachPair(writer, options, context, ((dynamic)arguments[0]).GetItems());
        }

        void ToBase64(EncodedTextWriter output, Context context, Arguments arguments)
        {
            var bytes = (byte[])arguments[0];

            if (bytes != null)
                output.Write(Convert.ToBase64String(bytes));
        }

        void footer(EncodedTextWriter writer, BlockHelperOptions options, Context context, Arguments arguments)
        {
            var viewBag = (IDictionary<string, object>)context["ViewBag"];

            if (viewBag.TryGetValue("ShowFooter", out var show) && (bool)show == true)
            {
                options.Template(writer, (object)context);
            }
        }

        void SilkierQuartzVersion(EncodedTextWriter  output, Context context, Arguments arguments)
        {
            var v = GetType().Assembly.GetCustomAttributes<AssemblyInformationalVersionAttribute>().FirstOrDefault();
            output.Write(v.InformationalVersion);
        }

        void Logo(EncodedTextWriter output, Context context, Arguments arguments)
        {
            output.Write(_services.Options.Logo);
        }
        void ProductName(EncodedTextWriter output, Context context, Arguments arguments)
        {
            output.Write(_services.Options.ProductName);
        }

        void CustomStyleSheet(EncodedTextWriter output, Context context, Arguments arguments)
        {
            output.Write(_services.Options.CustomStyleSheet);
        }

        void CustomFavicon(EncodedTextWriter output, Context context, Arguments arguments)
        {
            output.Write(_services.Options.CustomFavicon);
        }
    }
}
