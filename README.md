# **📇 CDN Freelancer Directory API**

A backend system for managing a directory of freelancers, built for the ETIQA IT Backend Developer Assessment (July 2025). This project follows Clean Architecture principles, uses ASP.NET Core Web API, Dapper ORM, and includes a React.js frontend for basic CRUD operations.

## 🚀 Tech Stack

| Layer            | Technology Used                              |
|------------------|----------------------------------------------|
| Backend API      | `ASP.NET Core Web API`                       |
| ORM              | `Dapper` *(no Entity Framework)*             |
| Database         | `SQL Server`                                 |
| Architecture     | Clean Architecture *(SOLID, SoC)*            |
| Frontend (Bonus) | `React.js`                                   |
| Testing          | `Postman`                              |
## 🔧 API Endpoints

| Method  | Endpoint                                  | Description                              |
|--------:|-------------------------------------------|------------------------------------------|
| `GET`   | `/api/freelancers`                        | List all freelancers                      |
| `GET`   | `/api/freelancers/{id}`                   | Get freelancer by ID                      |
| `POST`  | `/api/freelancers`                        | Register a new freelancer                 |
| `PUT`   | `/api/freelancers/{id}`                   | Full update of freelancer                 |
| `DELETE`| `/api/freelancers/{id}`                   | Delete freelancer                         |
| `GET`   | `/api/freelancers/search?q=<term>`        | Wildcard search by username/email         |
| `PATCH` | `/api/freelancers/{id}/archive`           | Archive freelancer                        |
| `PATCH` | `/api/freelancers/{id}/unarchive`         | Unarchive freelancer                      |
## 🖥️ Frontend (React.js)

**Basic UI**
- List freelancers  
- Add / update / delete freelancer  
- Archive / unarchive

**Run locally**
```bash
cd frontend/react-app
npm install
npm start

## 📂 How to Run Locally

### 1) Clone the repository
```bash
git clone https://github.com/koreyoshimy/Assessment.git
cd Assessment
