# API

## Story management

| Method | Path | Description |
| --- | --- | --- |
| GET | `/genres` | List all genres |
| GET | `/stories` | List stories with genre information |
| POST | `/stories` | Create a story |
| PUT | `/stories/{id}` | Update a story |
| DELETE | `/stories/{id}` | Delete a story |

## Office View and survey

| Method | Path | Description |
| --- | --- | --- |
| POST | `/stories/{id}/transform-for-office` | Transform a story temporarily for a selected office context |
| POST | `/stories/{id}/narrative-transportation` | Save survey responses and scoring metadata |
| POST | `/stories/{id}/improve-from-survey` | Improve the temporary transformed story using feedback guardrails |

## Example request

```json
{
  "officeName": "Law firm",
  "officeDescription": "A focused setting for reviewing a story alongside case notes and client objectives.",
  "responses": [5, 4, 3, 5, 4, 2, 1, 5, 4, 3, 5, 4, 2, 1, 5],
  "transformedStory": "Temporary story text",
  "storyVersion": "v1"
}
```
