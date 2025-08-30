using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text;

namespace MyLocalGov.com.Helpers
{
	public static class AnimationHelper
	{
		// ==========================
		// 🔹 AOS (Scroll Animations)
		// ==========================
		public static IHtmlContent ScrollAnimate(this IHtmlHelper html, string content, string animation = "fade-up", int delay = 0, int duration = 800)
		{
			var builder = new StringBuilder();
			builder.Append($"<div data-aos=\"{animation}\" data-aos-delay=\"{delay}\" data-aos-duration=\"{duration}\">");
			builder.Append(content);
			builder.Append("</div>");
			return new HtmlString(builder.ToString());
		}

		// ==========================
		// 🔹 Anime.js (Custom Animations)
		// ==========================
		private static IHtmlContent Anime(this IHtmlHelper html, string target, string animationJson)
		{
			var script = $@"
            <script>
                document.addEventListener('DOMContentLoaded', () => {{
                    anime(Object.assign({{ targets: '{target}' }}, {animationJson}));
                }});
            </script>";
			return new HtmlString(script);
		}

		// ==========================
		// 🔹 Anime.js Presets
		// ==========================

		public static IHtmlContent AnimeBounce(this IHtmlHelper html, string target)
		{
			var json = @"{
                scale: [
                    { value: 1.2, duration: 200 },
                    { value: 1, duration: 200 }
                ],
                easing: 'easeInOutQuad'
            }";
			return html.Anime(target, json);
		}

		public static IHtmlContent AnimeFadeIn(this IHtmlHelper html, string target)
		{
			var json = @"{
                opacity: [0, 1],
                duration: 800,
                easing: 'easeOutQuad'
            }";
			return html.Anime(target, json);
		}

		public static IHtmlContent AnimeSlideLeft(this IHtmlHelper html, string target)
		{
			var json = @"{
                translateX: [-100, 0],
                opacity: [0, 1],
                duration: 800,
                easing: 'easeOutExpo'
            }";
			return html.Anime(target, json);
		}

		public static IHtmlContent AnimePulse(this IHtmlHelper html, string target)
		{
			var json = @"{
                scale: [
                    { value: 1.05, duration: 200 },
                    { value: 1, duration: 200 }
                ],
                loop: true,
                direction: 'alternate',
                easing: 'easeInOutSine'
            }";
			return html.Anime(target, json);
		}

		// ==========================
		// 🔹 Page Transition Helpers
		// ==========================

		public static IHtmlContent PageFadeIn(this IHtmlHelper html)
		{
			var json = @"{
                targets: 'body',
                opacity: [0, 1],
                duration: 1000,
                easing: 'easeOutQuad'
            }";
			return html.Anime("body", json);
		}

		public static IHtmlContent PageFadeOutOnLeave(this IHtmlHelper html)
		{
			var script = @"
            <script>
                document.addEventListener('DOMContentLoaded', () => {
                    document.querySelectorAll('a').forEach(link => {
                        link.addEventListener('click', (e) => {
                            const href = link.getAttribute('href');
                            if (href && href.startsWith('/')) {
                                e.preventDefault();
                                anime({
                                    targets: 'body',
                                    opacity: [1, 0],
                                    duration: 500,
                                    easing: 'easeInQuad',
                                    complete: () => window.location.href = href
                                });
                            }
                        });
                    });
                });
            </script>";
			return new HtmlString(script);
		}
	}
}
