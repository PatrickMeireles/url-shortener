# Url Shortener

- Easy Shortner Url

## Endpoints
### [GET]/api/url/{code}
- **Response**
    - Redirect
    - NotFound

### [POST]/api/url/
``` 
{
  "url": ""
}
```

- **Response**
    - Created
    - BadRequest

## Tech
- .Net 8
    - Minimal Api
    - Dapper
- PostgreSql
- Redis