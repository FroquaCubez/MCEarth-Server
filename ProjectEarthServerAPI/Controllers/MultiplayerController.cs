using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using ProjectEarthServerAPI.Models.Buildplate;
using ProjectEarthServerAPI.Models.Multiplayer;
using ProjectEarthServerAPI.Util;
using Serilog;
using ProjectEarthServerAPI.Models.Multiplayer.Adventure;
using Asp.Versioning;
using ProjectEarthServerAPI.Models;

namespace ProjectEarthServerAPI.Controllers
{
	public class MultiplayerController : Controller
	{

		[Authorize]
		[ApiVersion("1.1")]
		[Route("1/api/v{version:apiVersion}/multiplayer/buildplate/{buildplateId}/instances")]
		public async Task<IActionResult> PostCreateInstance(string buildplateId)
		{
			return await ProcessInstanceRequest(buildplateId, MultiplayerUtils.CreateBuildplateInstance);
		}

		[Authorize]
		[ApiVersion("1.1")]
		[Route("1/api/v{version:apiVersion}/multiplayer/buildplate/{buildplateId}/play/instances")]
		public async Task<IActionResult> PostCreatePlayInstance(string buildplateId)
		{
			return await ProcessInstanceRequest(buildplateId, MultiplayerUtils.CreateBuildplatePlayInstance);
		}

		private async Task<IActionResult> ProcessInstanceRequest(string buildplateId, Func<string, string, Coordinate, Task<BuildplateServerResponse>> createInstanceFunc)
		{
			string authtoken = User.FindFirstValue(ClaimTypes.NameIdentifier);
			using (var streamReader = new StreamReader(Request.Body))
			{
				var body = await streamReader.ReadToEndAsync();
				var parsedRequest = JsonConvert.DeserializeObject<BuildplateServerRequest>(body);

				var response = await createInstanceFunc(authtoken, buildplateId, parsedRequest.playerCoordinate);
				return Content(JsonConvert.SerializeObject(response), "application/json");
			}
		}

		[Authorize]
		[ApiVersion("1.1")]
		[Route("1/api/v{version:apiVersion}/buildplates")]
		public IActionResult GetBuildplates()
		{
			string authtoken = User.FindFirstValue(ClaimTypes.NameIdentifier);

			var response = BuildplateUtils.GetBuildplatesList(authtoken);
			return Content(JsonConvert.SerializeObject(response), "application/json");
		}

		[Authorize]
		[ApiVersion("1.1")]
		[Route("1/api/v{version:apiVersion}/buildplates/{buildplateId}/share")]
		public IActionResult ShareBuildplate(string buildplateId)
		{
			string authtoken = User.FindFirstValue(ClaimTypes.NameIdentifier);
			var response = BuildplateUtils.ShareBuildplate(Guid.Parse(buildplateId), authtoken);
			return Content(JsonConvert.SerializeObject(response), "application/json");
		}

		[Authorize]
		[ApiVersion("1.1")]
		[Route("1/api/v{version:apiVersion}/buildplates/shared/{buildplateId}")]
		public IActionResult GetSharedBuildplate(string buildplateId)
		{
			string authtoken = User.FindFirstValue(ClaimTypes.NameIdentifier);
			var response = BuildplateUtils.ReadSharedBuildplate(buildplateId);
			return Content(JsonConvert.SerializeObject(response), "application/json");
		}

		[Authorize]
		[ApiVersion("1.1")]
		[Route("1/api/v{version:apiVersion}/multiplayer")]
		public class MultiplayerBuildplateController : ControllerBase
		{
			private string GetAuthToken() => User.FindFirstValue(ClaimTypes.NameIdentifier);

			private async Task<T> DeserializeRequestBody<T>()
			{
				using var streamReader = new StreamReader(Request.Body);
				var body = await streamReader.ReadToEndAsync();
				return JsonConvert.DeserializeObject<T>(body);
			}

			private IActionResult SerializeResponse(object response)
			{
				return Content(JsonConvert.SerializeObject(response), "application/json");
			}

			[HttpPost("buildplate/shared/{buildplateId}/play/instances")]
			public async Task<IActionResult> PostSharedBuildplateCreatePlayInstance(string buildplateId)
			{
				string authtoken = GetAuthToken();
				var parsedRequest = await DeserializeRequestBody<SharedBuildplateServerRequest>();

				var response = await MultiplayerUtils.CreateSharedBuildplatePlayInstance(authtoken, buildplateId, parsedRequest.playerCoordinate);
				return SerializeResponse(response);
			}

			[HttpPost("join/instances")]
			public async Task<IActionResult> PostMultiplayerJoinInstance()
			{
				string authtoken = GetAuthToken();
				var parsedRequest = await DeserializeRequestBody<MultiplayerJoinRequest>();
				Log.Information($"[{authtoken}]: Trying to join buildplate instance: id {parsedRequest.id}");

				var response = MultiplayerUtils.GetServerInstance(parsedRequest.id);
				return SerializeResponse(response);
			}

			[HttpPost("encounters/{adventureid}/instances")]
			public async Task<IActionResult> PostCreateEncounterInstance(string adventureid)
			{
				string authtoken = GetAuthToken();
				var parsedRequest = await DeserializeRequestBody<EncounterServerRequest>();

				var response = await MultiplayerUtils.CreateAdventureInstance(authtoken, adventureid, parsedRequest.playerCoordinate);
				return SerializeResponse(response);
			}

			[HttpPost("adventures/{adventureid}/instances")]
			[HttpPost("player/adventures/{adventureid}/instances")]
			public async Task<IActionResult> PostCreateAdventureInstance(string adventureid)
			{
				string authtoken = GetAuthToken();
				var parsedRequest = await DeserializeRequestBody<BuildplateServerRequest>();

				var response = await MultiplayerUtils.CreateAdventureInstance(authtoken, adventureid, parsedRequest.playerCoordinate);
				return SerializeResponse(response);
			}

			[HttpPost("encounters/state")]
			public async Task<IActionResult> EncounterState()
			{
				string authtoken = GetAuthToken();
				var request = await DeserializeRequestBody<Dictionary<Guid, string>>();
				var response = new EncounterStateResponse { result = new Dictionary<Guid, ActiveEncounterStateMetadata> { { Guid.Parse("b7335819-c123-49b9-83fb-8a0ec5032779"), new ActiveEncounterStateMetadata { ActiveEncounterState = ActiveEncounterState.Dirty } } }, expiration = null, continuationToken = null, updates = null };
				return SerializeResponse(response);
			}
		}

		[Authorize]
		[ApiVersion("1.1")]
		[Route("1/api/v{version:apiVersion}/multiplayer/partitions/{worldId}/instances/{instanceId}")]
		public IActionResult GetInstanceStatus(string worldId, Guid instanceId)
		{
			string authtoken = User.FindFirstValue(ClaimTypes.NameIdentifier);

			var response = MultiplayerUtils.CheckInstanceStatus(authtoken, instanceId);
			if (response == null)
			{
				return StatusCode(204);
			}
			else
			{
				return Content(JsonConvert.SerializeObject(response), "application/json");
			}
		}

		[ApiVersion("1.1")]
		[Route("1/api/v{version:apiVersion}/private/server/command")]
		public async Task<IActionResult> PostServerCommand()
		{
			var stream = new StreamReader(Request.Body);
			var body = await stream.ReadToEndAsync();
			var parsedRequest = JsonConvert.DeserializeObject<ServerCommandRequest>(body);

			var response = MultiplayerUtils.ExecuteServerCommand(parsedRequest);

			if (response == "ok") return Ok();
			else return Content(response, "application/json");
		}

		[ApiVersion("1.1")]
		[Route("1/api/v{version:apiVersion}/private/server/ws")]
		public async Task GetWebSocketServer()
		{
			if (HttpContext.WebSockets.IsWebSocketRequest)
			{
				var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
				await MultiplayerUtils.AuthenticateServer(webSocket);
			}
		}
	}
}
