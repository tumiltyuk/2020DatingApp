using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DatingApp.API.Helpers;
using DatingApp.API.Models;
using Microsoft.EntityFrameworkCore;

namespace DatingApp.API.Data
{
    public class DatingRepository : IDatingRepository
    {
        private readonly DataContext _context;

        public DatingRepository(DataContext context)
        {
            _context = context;
        }
        public void Add<T>(T entity) where T : class
        {
            _context.Add(entity);
        }

        public void Delete<T>(T entity) where T : class
        {
            _context.Remove(entity);
        }

        public async Task<Like> GetLike(int userId, int recipientId)
        {
            return await _context.Likes.FirstOrDefaultAsync(u => 
                u.LikerId == userId && u.LikeeId == recipientId);
        }

        public async Task<Photo> GetMainPhotoForUser(int userId)
        {
            return await _context.Photos.Where(u => u.UserId == userId)
                .FirstOrDefaultAsync(p => p.IsMain);
        }

        public async Task<Photo> GetPhoto(int id)
        {
            var photo = await _context.Photos.FirstOrDefaultAsync(p => p.Id == id);

            return photo;
        }

        public async Task<User> GetUser(int id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);

            return user;
        }

        public async Task<PagedList<User>> GetUsers(UserParams userParams)
        {
            var users = _context.Users.OrderByDescending(u => u.LastActive).AsQueryable();

            // --- Query Conditions ---
            users = users.Where(u => u.Id != userParams.UserId); // dont return the currently logged on user profile
            users = users.Where(u => u.Gender == userParams.Gender); // Dont return same sex matches (userParams contain oposite sex to user) --- TODO - need to fix this in future work
            
            if (userParams.Likers)
            {
                var userLikers = await GetUserLikesList(userParams.UserId, userParams.Likers);
                users = users.Where(u => userLikers.Contains(u.Id));
            }
            if (userParams.Likees)
            {
                var userLikees = await GetUserLikesList(userParams.UserId, userParams.Likers);
                users = users.Where(u => userLikees.Contains(u.Id));
            }

            if (userParams.MinAge != 18 || userParams.MaxAge != 99) 
            {
                var minDob = DateTime.Today.AddYears(- userParams.MaxAge - 1);
                var maxDob = DateTime.Today.AddYears(- userParams.MinAge);
                users = users.Where(u => u.DateOfBirth >= minDob && u.DateOfBirth <= maxDob);
            }
            if (!string.IsNullOrEmpty(userParams.OrderBy))
            {
                switch (userParams.OrderBy) 
                {
                    case "created": 
                        users = users.OrderByDescending(u => u.Created);
                        break;
                        default:
                        users = users.OrderByDescending(u => u.LastActive);
                        break;
                }
            }

            return await PagedList<User>.CreateAsync(users, userParams.PageNumber, userParams.PageSize);
        }

        private async Task<IEnumerable<int>> GetUserLikesList(int userId, bool likers)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (likers) // return people who have "liked" the currently logged in user
            {
                return user.Likers
                    .Where(u => u.LikeeId == userId)
                    .Select(i => i.LikerId);
            }
            else // return people who the currently logged in user has "liked" 
            {
                return user.Likees
                    .Where(u => u.LikerId == userId)
                    .Select(u => u.LikeeId);
            }
        }

        public async Task<bool> SaveAll()
        {   // if something saved to db then SaveChangesAsync will return 1 "true", otherwise it will return 0 "false"
            return await _context.SaveChangesAsync() > 0;            
        }

        public async Task<Message> GetMessage(int id)
        {
            return await _context.Messages.FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<PagedList<Message>> GetMessagesForUser(MessageParams messageParams)
        {
            // Get ALL Messages
            var messages = _context.Messages.AsQueryable();

            // Then Filer out Results
            switch (messageParams.MessageContainer)
            {
                case "Inbox":       // Messages for Recipient (current user)
                    messages = messages.Where(u => u.RecipientId == messageParams.UserId
                                                && u.RecipientDeleted == false);
                break;
                case "Outbox":      // Messages for Sender - (sent items by current user)
                    messages = messages.Where(u => u.SenderId == messageParams.UserId
                                                && u.SenderDeleted == false);
                break;
                default:            // Messages for Recipient (current User) and is not a read message
                    messages = messages.Where(u => u.RecipientId == messageParams.UserId
                                                    && u.RecipientDeleted == false 
                                                    && u.IsRead == false);
                break;
            }

            // Order messages
            messages = messages.OrderByDescending(d => d.MessageSent);

            return await PagedList<Message>.CreateAsync(messages, messageParams.PageNumber, messageParams.PageSize);
        }

        public async Task<IEnumerable<Message>> GetMessageThread(int userId, int recipientId)
        {
            // Get ALL Messages
            var messages = await _context.Messages
                .Where(m => m.RecipientId == userId         // Return Messages WHERE recipient is the current logged on user (Recived messages)
                            && m.SenderId == recipientId        // Where the current logged on user is the recipient
                            && m.RecipientDeleted == false      // Where the recipient has not deleted the message

                        || m.RecipientId == recipientId     // OR Where the current user has sent a message to a receipient (Sent messages)
                            && m.SenderId == userId             // Where the current user IS The sender
                            && m.SenderDeleted == false)        // Where the Sender has NOT deleted the message
                .OrderByDescending(m => m.MessageSent)
                .ToListAsync();

            return messages;
        }
    }
}