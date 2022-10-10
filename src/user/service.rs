use crate::user::bean::user::User;
use crate::user::mapper;
use rbatis::db::DBExecResult;

pub async fn save(user: &User) -> Result<DBExecResult, rbatis::Error> {
    mapper::save(user).await
}
