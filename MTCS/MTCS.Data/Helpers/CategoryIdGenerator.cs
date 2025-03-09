using MTCS.Data;

namespace MTCS.Data.Helpers
{
    public static class CategoryIDGenerator
    {
        public static async Task<string> GenerateCategoryId(UnitOfWork unitOfWork)
        {
            int nextId = 1;
            List<string> existingIds = new List<string>();

            var tractorCategories = await unitOfWork.TractorRepository.GetAllTractorCategories();
            var trailerCategories = await unitOfWork.TrailerRepository.GetAllTrailerCategories();

            var tractorIds = tractorCategories.Select(c => c.TractorCateId);
            var trailerIds = trailerCategories.Select(c => c.TrailerCateId);

            existingIds.AddRange(tractorIds);
            existingIds.AddRange(trailerIds);

            // Process all IDs that are numeric
            var numericValues = existingIds
                .Where(id => int.TryParse(id, out _))
                .Select(id => int.Parse(id));

            if (numericValues.Any())
                nextId = numericValues.Max() + 1;

            string newId = $"{nextId:D3}";

            while (existingIds.Contains(newId))
            {
                nextId++;
                newId = $"{nextId:D3}";
            }

            return newId;
        }
    }
}
