# TestChunkedUpload
POC to support ASP.NET chunked uploads (to overcome the upload size limitation)

Usage:

`POST https://localhost:44368/api/upload`

Body uses `form-data`

- `file`: file chunk to upload
- `index`: index of the chunk
- `totalCount`: number of chunks

It saves the chunks in temp folder and then merges them in the last call

If called multiple times with same index it will replace the chunk
