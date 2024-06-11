using MessengerInterfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Newtonsoft.Json;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Container = Microsoft.Azure.Cosmos.Container;

namespace Messenger.Controllers
{

    //[ApiController]
    //[Route("[controller]")]
    public class MessengerController : ControllerBase
    {

        private readonly ILogger<MessengerController> _logger;
        private readonly IMessengerService messengerService;


        public MessengerController(ILogger<MessengerController> logger, IMessengerService messengerService)
        {
            _logger = logger;
            this.messengerService = messengerService;
        }

        [HttpPost("editAccountAdminStatus")]
        public async Task<ActionResult> EditAccountAdminStatus([FromQuery, Required] bool giveAdmin, [FromQuery, Required] Guid userId = default, [FromBody, Required] Guid Id = default)
        {
            try
            {
                await messengerService.EditAccountAdmin(Id, giveAdmin, userId);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("getAllMessages")]
        public async Task<ActionResult<MessageDTO_Output[]>> GetAllMessages([FromQuery, Required] Guid userId)
        {
            try
            {
                return await messengerService.GetAllMessages(userId);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{channelId}/getAllMessages")]
        public async Task<ActionResult<MessageDTO_Output[]>> GetAllMessagesInChannel([FromRoute, Required]Guid channelId, [FromQuery, Required] Guid userId)
        {
            try
            {
                return await messengerService.GetAllMessagesInChannel(channelId, userId);
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet("getAccount")]
        public async Task<ActionResult<Account>> GetAccount([FromQuery, Required] Guid Id, [FromQuery, Required] Guid userId)
        {
            try
            {
                return await messengerService.GetAccount(Id);
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }
        
        [HttpGet("getChannel")]
        public async Task<ActionResult<ChannelDTO_Output>> GetChannel([FromQuery, Required] Guid channelId, [FromQuery, Required] Guid userId)
        {
            try
            {
                ChannelDTO_Output channel = await messengerService.GetChannel(channelId, userId);
                return channel;
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPost("{channelId}/addUser")]
        public async Task<ActionResult> AddUser([FromBody, Required] Guid Id, [FromRoute, Required] Guid channelId, [FromQuery, Required] Guid userId)
        {
            try
            {
                await messengerService.AddUserToChannel(Id, channelId, userId);
                return Ok();
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{channelId}/postMessage")]
        public async Task<ActionResult> PostMessage([FromBody, Required] MessageDTO_Input msg, [FromRoute, Required] Guid channelId, [FromQuery, Required] Guid userId)
        {
            try
            {
                Message message = msg.ToMessage(userId, channelId);
                await messengerService.PostMessage(message, userId);
                return Ok();
            }
            catch(Exception ex) 
            { 
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("createAccount")]
        public async Task<ActionResult<Guid>> CreateAccount([FromBody, Required] string username, string passwordHash)
        {
            try
            {
                return await messengerService.CreateAccount(username, passwordHash);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("createChannel")]
        public async Task<ActionResult<Guid>> CreateChannel([FromBody, Required] HashSet<Guid> userIds, [FromQuery, Required] Guid userId)
        {
            try
            {
                return await messengerService.CreateChannel(userIds, userId, "");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}